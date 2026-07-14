using ActionEventServiceNS;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Converters;
using ReswareConnectorWeb.Extensions;
using ReswareConnectorWeb.Models;
using ReswareConnectorWeb.RawDataModels;
using ReswareConnectorWeb.Services;
using SearchDataServiceNS;
using System.IO.Compression;
using System.Text.Json;

namespace ReswareConnectorWeb.Controllers
{
    [Route("reswareconnector/api/v{version:apiVersion}")]
    [ApiVersion("1.0")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly IValidator<FlexibleOrderDto> _validator;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IIntegrationService _integrationService;
        private readonly ILogger<IntegrationController> _logger;
        private readonly FileStorageConfig _config;
        public IntegrationController(IIntegrationService integrationService, IValidator<FlexibleOrderDto> validator, ILogger<IntegrationController> logger, IOptions<FileStorageConfig> config)
        {
            _integrationService = integrationService;
            _logger = logger;
            _validator = validator;
            _config = config.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                Converters = { new ReceiveCurativeTypeEnumConverter() }
            };
        }

        [HttpPost]
        [Route("order")]
        public async Task<ActionResult<OrderResponseDto>> PostOrder([FromBody] FlexibleOrderDto order)
        {
            try
            {
                // Step 1: Store original JSON strings for error reporting
                string? noteDataJson = null;
                string? searchDataJson = null;
                string? actionEventDataJson = null;

                if (order.NoteData != null)
                    noteDataJson = JsonSerializer.Serialize(order.NoteData);

                if (order.SearchData != null)
                    searchDataJson = JsonSerializer.Serialize(order.SearchData);

                if (order.ActionEventData != null)
                    actionEventDataJson = JsonSerializer.Serialize(order.ActionEventData);

                // Step 2: Validate
                var validationResult = await _validator.ValidateAsync(order);

                if (!validationResult.IsValid)
                {
                    // Step 3: Build errors with attempted values
                    var errors = new List<ValidationError>();

                    foreach (var failure in validationResult.Errors)
                    {
                        string? attemptedValue = null;

                        // Extract attempted value from the appropriate JSON
                        if (failure.PropertyName.StartsWith("NoteData."))
                        {
                            attemptedValue = GetValueFromJsonPath(noteDataJson, failure.PropertyName.Replace("NoteData.", ""));
                        }
                        else if (failure.PropertyName.StartsWith("SearchData."))
                        {
                            attemptedValue = GetValueFromJsonPath(searchDataJson, failure.PropertyName.Replace("SearchData.", ""));
                        }
                        else if (failure.PropertyName.StartsWith("ActionEventData."))
                        {
                            attemptedValue = GetValueFromJsonPath(actionEventDataJson, failure.PropertyName.Replace("ActionEventData.", ""));
                        }

                        errors.Add(new ValidationError
                        {
                            Property = failure.PropertyName,
                            Error = failure.ErrorMessage,
                            AttemptedValue = attemptedValue
                        });
                    }

                    return BadRequest(new OrderResponseDto
                    {
                        ErrorMessage = "Input Validation failed",
                        ValidationErrors = errors
                    });
                }

                // Step 4: Process valid data
                var validatedOrder = new OrderDto
                {
                    SendNoteData = order.SendNoteData,
                    SendSearchData = order.SendSearchData,
                    SendActionEventData = order.SendActionEventData,
                    FileID = order.FileID
                };

                // Deserialize NoteData with enum converter
                if (order.NoteData != null && noteDataJson != null)
                {
                    var noteOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new ReceiveCurativeTypeEnumConverter() }
                    };
                    validatedOrder.NoteData = JsonSerializer.Deserialize<ReceiveNoteRequestDto>(noteDataJson, noteOptions);
                }

                // Deserialize other data
                if (order.SearchData != null && searchDataJson != null)
                {
                    validatedOrder.SearchData = JsonSerializer.Deserialize<ReceiveSearchDataDataDto>(searchDataJson, _jsonOptions);
                }

                if (order.ActionEventData != null && actionEventDataJson != null)
                {
                    validatedOrder.ActionEventData = JsonSerializer.Deserialize<ReceiveActionEventData>(actionEventDataJson, _jsonOptions);
                }

                var validationError = string.Empty;
                if (validatedOrder.SendNoteData && validatedOrder.SendSearchData && validatedOrder.NoteData?.FileNumber != validatedOrder.SearchData?.FileNumber)
                    validationError = "FileNumber Mismatch between NoteDocument Data & SearchData Data";
                else if (validatedOrder.SendNoteData && validatedOrder.SendActionEventData && validatedOrder.NoteData?.FileNumber != validatedOrder.ActionEventData?.FileNumber)
                    validationError = "FileNumber Mismatch between NoteDocument Data & ActionEventData Data";
                else if (validatedOrder.SendSearchData && validatedOrder.SendActionEventData && validatedOrder.SearchData?.FileNumber != validatedOrder.ActionEventData?.FileNumber)
                    validationError = "FileNumber Mismatch between SearchData Data & ActionEventData Data";

                if (!string.IsNullOrEmpty(validationError))
                {
                    return BadRequest(new OrderResponseDto
                    {
                        ErrorMessage = validationError
                    });
                }
                // Process the validated order...
                var response = await _integrationService.PostOrderAsync(validatedOrder);
                return Ok(response);
            }
            catch (JsonException ex)
            {
                // This shouldn't happen after validation, but just in case
                return BadRequest(new { Message = "Invalid data format", Details = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Unknow error.", Details = ex.Message });
            }
        }

        private string? GetValueFromJsonPath(string json, string propertyPath)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyPath))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var element = doc.RootElement;

                // Split path by dots
                var pathParts = propertyPath.Split('.');

                for (int i = 0; i < pathParts.Length; i++)
                {
                    var part = pathParts[i];

                    // Check if this part has array index like "Documents[0]"
                    if (part.Contains('[') && part.EndsWith(']'))
                    {
                        var arrayName = part.Substring(0, part.IndexOf('['));
                        var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                        if (!int.TryParse(indexStr, out int index))
                            return null;

                        // Get the array
                        if (!element.TryGetPropertyCaseInsensitive(arrayName, out element, _jsonOptions))
                            return null;

                        // Get array element at index
                        if (element.ValueKind != JsonValueKind.Array || index >= element.GetArrayLength())
                            return null;

                        element = element[index];
                    }
                    else
                    {
                        // Regular property
                        if (!element.TryGetPropertyCaseInsensitive(part, out element, _jsonOptions))
                            return null;
                    }
                }

                // Convert to string
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "null",
                    JsonValueKind.Object => "{...}",
                    JsonValueKind.Array => "[...]",
                    _ => element.GetRawText()
                };
            }
            catch
            {
                return null;
            }
        }
        /*
        [HttpPost]
        [Route("order")]
        public async Task<ActionResult<OrderResponseDto>> PostOrder([FromBody] OrderDto order)
        {
            _logger.LogInformation("PostOrder request received");
            string? validationError = null;

            if (order == null)
                validationError = "Order JSON is required";
            else if (order.NoteData == null && order.SearchData == null && order.ActionEventData == null)
                validationError = "Any of NoteData, SearchData and ActionEvent is required";
            else
            {
                if (order.SendNoteData)
                {
                    if (order.NoteData == null)
                        validationError = "NoteDocument Data is required";
                    else if (string.IsNullOrEmpty(order.NoteData.FileNumber))
                        validationError = "Invalid FileNumber in NoteDocument Data";
                    else if (!TryValidateModel(order.NoteData))
                        validationError = "Invalid NoteDocument Data";
                }
                if (validationError == null && order.SendSearchData)
                {
                    if (order.SearchData == null)
                        validationError = "SearchData Data is required";
                    else if (string.IsNullOrEmpty(order.SearchData.FileNumber))
                        validationError = "Invalid FileNumber in SearchData Data";
                    else if (!TryValidateModel(order.SearchData))
                        validationError = "Invalid SearchData Data";
                }
                if (validationError == null && order.SendActionEventData)
                {
                    if (order.ActionEventData == null)
                        validationError = "ActionEventData is required";
                    else if (string.IsNullOrEmpty(order.ActionEventData.FileNumber))
                        validationError = "Invalid FileNumber in ActionEventData Data";
                    else if (!TryValidateModel(order.ActionEventData))
                        validationError = "Invalid ActionEventData Data";
                }
                if (validationError == null && order.SendNoteData && order.SendSearchData && order.NoteData?.FileNumber != order.SearchData?.FileNumber)
                    validationError = "FileNumber Mismatch between NoteDocument Data & SearchData Data";
                else if (validationError == null && order.SendNoteData && order.SendActionEventData && order.NoteData?.FileNumber != order.ActionEventData?.FileNumber)
                    validationError = "FileNumber Mismatch between NoteDocument Data & ActionEventData Data";
                else if (validationError == null && order.SendSearchData && order.SendActionEventData && order.SearchData?.FileNumber != order.ActionEventData?.FileNumber)
                    validationError = "FileNumber Mismatch between SearchData Data & ActionEventData Data";
            }
            try
            {
                if (validationError == null)
                {
                    var response = await _integrationService.PostOrderAsync(order);
                    return Ok(response);
                }
                else
                {
                    return StatusCode(422, new OrderResponseDto
                    {
                        ErrorMessage = validationError
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReceiveNoteResponseDto
                {
                    Message = $"Internal server error: {ex.Message}",
                });
            }
        }


        [HttpPost]
        [Route("notedocument")]
        public async Task<ActionResult<ReceiveNoteResponseDto>> ReceiveNoteDocument([FromForm] string request, [FromForm] List<IFormFile> files)
        {
            _logger.LogInformation("ReceiveNotDocument request received");
            if (string.IsNullOrEmpty(request))
            {
                return BadRequest(new ReceiveNoteResponseDto
                {
                    Message = "Request JSON is required",
                    ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                });
            }

            try
            {
                var requestDto = request.FromJson<ReceiveNoteData>();
                if (requestDto == null)
                {
                    return BadRequest(new ReceiveNoteResponseDto
                    {
                        Message = "Invalid JSON data",
                        ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                    });
                }

                // Validate the deserialized model
                if (!TryValidateModel(requestDto))
                {
                    return BadRequest(new ReceiveNoteResponseDto
                    {
                        Message = "Invalid request data",
                        ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                    });
                }

                var response = await _integrationService.ReceiveNoteAsync(requestDto, files);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReceiveNoteResponseDto
                {
                    Message = $"Internal server error: {ex.Message}",
                    ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                });
            }
        }

        [HttpPost]
        [Route("searchdata")]
        public async Task<ActionResult<ReceiveSearchDataResponseDto>> ReceiveSearchData([FromBody] object request)
        {
            _logger.LogInformation("ReceiveSearchData request received");
            if (request == null)
            {
                return BadRequest(new ReceiveSearchDataResponseDto
                {
                    Message = "Request JSON is required",
                    ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                });
            }

            try
            {
                var requestJson = JsonSerializer.Serialize(request);
                var requestDto = requestJson.FromJson<ReceiveSearchDataData>();
                if (requestDto == null)
                {
                    return BadRequest(new ReceiveSearchDataResponseDto
                    {
                        Message = "Invalid JSON data",
                        ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                    });
                }

                //Validate the deserialized model
                if (!TryValidateModel(requestDto))
                {
                    return BadRequest(new ReceiveSearchDataResponseDto
                    {
                        Message = "Invalid request data",
                        ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                    });
                }

                var response = await _integrationService.ReceiveSearchDataAsync(requestDto);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReceiveSearchDataResponseDto
                {
                    Message = $"Internal server error: {ex.Message}",
                    ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                });
            }
        }
        */

        [HttpPost]
        [Route("actionevent")]
        public async Task<ActionResult<ReceiveActionEventResponseDto>> ReceiveActionEvent([FromBody] ReceiveActionEventData request)
        {
            _logger.LogInformation("ReceiveActionEvent request received");
            string? validationError = null;
            if (request == null)
                validationError = "Request JSON is required";
            else if (!TryValidateModel(request))
                validationError = "Invalid request data";

            try
            {
                if (validationError == null)
                {
                    var response = await _integrationService.ReceiveActionEventAsync(request);
                    return Ok(response);
                }
                else
                {
                    return StatusCode(422, new OrderResponseDto
                    {
                        ErrorMessage = validationError
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ReceiveActionEventResponseDto
                {
                    Message = $"Internal server error: {ex.Message}",
                });
            }
        }

        [HttpGet]
        [Route("transaction/data/{transactionReferenceNumber}")]
        public async Task<ActionResult<object>> GetTransactionDataAsync(long transactionReferenceNumber)
        {
            if (transactionReferenceNumber < 0)
            {
                return StatusCode(422, "Invalid TransactionReferenceNumber");
            }
            try
            {
                var response = await _integrationService.GetTransactionDataAsync(transactionReferenceNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("transaction/history/{fileNumber}")]
        public async Task<ActionResult<TransactionHistory>> GetTransactionHistoryAsync(string fileNumber)
        {
            if (string.IsNullOrEmpty(fileNumber))
            {
                return StatusCode(422, "FileNumber is required");
            }
            try
            {
                var response = await _integrationService.GetTransactionHistoryByFileNumber(fileNumber);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /*
        [HttpGet]
        [Route("transaction/request/download/{fileNumber}")]
        [Produces("application/xml")]
        public async Task<IActionResult> DownloadRequestAsync(string fileNumber)
        {
            try
            {
                var fileData = await _integrationService.DownloadRequestAsync(fileNumber);

                if (fileData == null || fileData.Length == 0)
                {
                    return NotFound($"No request data found for FileNumber: {fileNumber}");
                }

                return File(fileData, "application/xml", "request.xml");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("transaction/response/download/{fileNumber}")]
        [Produces("application/xml")]
        public async Task<IActionResult> DownloadResponseAsync(string fileNumber, TransactionTypeEnum documentType)
        {
            try
            {
                if(documentType == TransactionTypeEnum.Order)
                {
                    return NotFound($"Transaction Type 'Order' is not allowed.");
                }
                var fileData = await _integrationService.DownloadResponseAsync(fileNumber, documentType);

                if (fileData == null || fileData.Length == 0)
                {
                    return NotFound($"No response data found for FileNumber: {fileNumber}");
                }

                return File(fileData, "application/xml", $"response_{documentType.ToString()}.xml");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        */

        [HttpGet]
        [Route("transaction/history/download/{fileNumber}")]
        [Produces("application/zip")]
        public IActionResult DownloadZipStream(string fileNumber)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, fileNumber);

            if (!Directory.Exists(targetPath))
                return NotFound($"Folder '{fileNumber}' not found");

            var files = Directory.GetFiles(targetPath);
            if (!files.Any())
                return BadRequest("No files to zip");

            var memoryStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var entryName = Path.GetFileName(file);
                    var entry = zipArchive.CreateEntry(entryName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = System.IO.File.OpenRead(file))
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream, "application/zip", $"{fileNumber}.zip");
        }
    }
}
