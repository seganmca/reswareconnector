using ActionEventServiceNS;
using FluentValidation;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Models;
using ReswareConnectorWeb.RawDataModels;
using ReswareConnectorWeb.ReswareServices;
using SearchDataServiceNS;
using System.Text.Json;
using ReswareConnectorWeb.Extensions;
using System.Text.Json.Serialization;

namespace ReswareConnectorWeb.Validators
{
    public class OrderDtoValidator : AbstractValidator<FlexibleOrderDto>
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public OrderDtoValidator()
        {
            RuleFor(x => x).Custom(ValidateOrder);
        }

        private void ValidateOrder(FlexibleOrderDto order, ValidationContext<FlexibleOrderDto> context)
        {
            // Validate Send flags
            if (order.SendNoteData && order.NoteData == null)
            {
                context.AddFailure("NoteData", "NoteData is required when SendNoteData is true");
            }

            if (order.SendSearchData && order.SearchData == null)
            {
                context.AddFailure("SearchData", "SearchData is required when SendSearchData is true");
            }

            if (order.SendActionEventData && order.ActionEventData == null)
            {
                context.AddFailure("ActionEventData", "ActionEventData is required when SendActionEventData is true");
            }

            if (order.NoteData == null && order.SearchData == null && order.ActionEventData == null)
                context.AddFailure("OrderData", "Any of NoteData, SearchData and ActionEvent is required");

            // Validate each data object
            if (order.NoteData != null)
            {
                ValidateNoteData(order.NoteData, context, "NoteData");
            }
            if (order.SearchData != null)
            {
                ValidateSearchData(order.SearchData, context, "SearchData");
            }

            if (order.ActionEventData != null)
            {
                ValidateActionEventData(order.ActionEventData, context, "ActionEventData");
            }
        }

        private void ValidateNoteData(object noteData, ValidationContext<FlexibleOrderDto> context, string propertyPath)
        {
            try
            {
                var json = JsonSerializer.Serialize(noteData);
                var element = JsonSerializer.Deserialize<JsonElement>(json);

                // Validate required fields
                ValidateRequiredString(element, "FileNumber", propertyPath, context, maxLength: 100);

                // Validate CurativeID
                if (element.TryGetPropertyCaseInsensitive("CurativeID", out var curativeId, _jsonOptions))
                {
                    try
                    {
                        if (!curativeId.TryGetInt32(out _))
                        {
                            context.AddFailure($"{propertyPath}.CurativeID", "CurativeID must be an integer");
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{propertyPath}.CurativeID", $"CurativeID must be a valid integer: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{propertyPath}.CurativeID", "CurativeID is required");
                }

                // Validate CurativeType enum
                if (element.TryGetPropertyCaseInsensitive("CurativeType", out var curativeType, _jsonOptions))
                {
                    if (curativeType.ValueKind == JsonValueKind.String)
                    {
                        var typeStr = curativeType.GetString();
                        if (string.IsNullOrEmpty(typeStr))
                        {
                            context.AddFailure($"{propertyPath}.CurativeType", "CurativeType cannot be empty");
                        }
                        else if (!Enum.TryParse<ReceiveCurativeTypeEnum>(typeStr, true, out _))
                        {
                            context.AddFailure($"{propertyPath}.CurativeType",
                                $"Invalid CurativeType. Must be 'PRE_CLOSING' (or 0) or 'POLICY' (or 1)");
                        }
                    }
                    else if (curativeType.ValueKind == JsonValueKind.Number)
                    {
                        try
                        {
                            var intValue = curativeType.GetInt32();
                            if (intValue != 0 && intValue != 1)
                            {
                                context.AddFailure($"{propertyPath}.CurativeType",
                                    "CurativeType must be 0 (PRE_CLOSING) or 1 (POLICY)");
                            }
                        }
                        catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                        {
                            context.AddFailure($"{propertyPath}.CurativeType",
                                $"CurativeType must be a valid integer (0 or 1): {ex.Message}");
                        }
                    }
                    else if (curativeType.ValueKind != JsonValueKind.Null)
                    {
                        context.AddFailure($"{propertyPath}.CurativeType",
                            "CurativeType must be a string ('PRE_CLOSING' or 'POLICY') or integer (0 or 1)");
                    }
                }
                else
                {
                    context.AddFailure($"{propertyPath}.CurativeType", "CurativeType is required");
                }

                // Validate ToCoordinatorID
                if (element.TryGetPropertyCaseInsensitive("ToCoordinatorID", out var toCoordinatorId, _jsonOptions))
                {
                    try
                    {
                        if (!toCoordinatorId.TryGetInt32(out _))
                        {
                            context.AddFailure($"{propertyPath}.ToCoordinatorID", "ToCoordinatorID must be an integer");
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{propertyPath}.ToCoordinatorID", $"ToCoordinatorID must be a valid integer: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{propertyPath}.ToCoordinatorID", "ToCoordinatorID is required");
                }

                // Validate optional fields
                ValidateNullableInt(element, "CoordinatorTypeID", propertyPath, context);
                ValidateString(element, "NoteSubject", propertyPath, context, isRequired: false, maxLength: 500);
                ValidateString(element, "NoteBody", propertyPath, context, isRequired: false);

                // Validate documents array if present
                if (element.TryGetPropertyCaseInsensitive("Documents", out var documents, _jsonOptions))
                {
                    if (documents.ValueKind == JsonValueKind.Array)
                    {
                        ValidateDocumentsArray(documents, context, $"{propertyPath}.Documents");
                    }
                    else if (documents.ValueKind != JsonValueKind.Null)
                    {
                        context.AddFailure($"{propertyPath}.Documents", "Documents must be an array");
                    }
                }
            }
            catch (JsonException ex)
            {
                context.AddFailure(propertyPath, $"Invalid JSON format: {ex.Message}");
            }
        }

        private void ValidateSearchData(object searchData, ValidationContext<FlexibleOrderDto> context, string propertyPath)
        {
            try
            {
                var json = JsonSerializer.Serialize(searchData);
                var element = JsonSerializer.Deserialize<JsonElement>(json);

                // Validate required fields
                ValidateRequiredString(element, "FileNumber", propertyPath, context, isRequired: true);

                // Validate ServiceVersion is required integer
                if (element.TryGetPropertyCaseInsensitive("ServiceVersion", out var serviceVersion, _jsonOptions))
                {
                    try
                    {
                        if (!serviceVersion.TryGetInt32(out _))
                        {
                            context.AddFailure($"{propertyPath}.ServiceVersion", "ServiceVersion must be an integer");
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{propertyPath}.ServiceVersion", $"ServiceVersion must be a valid integer: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{propertyPath}.ServiceVersion", "ServiceVersion is required");
                }

                // Validate nullable value types
                ValidateNullableDecimal(element, "AssessedImprovementValue", propertyPath, context);
                ValidateNullableDecimal(element, "AssessedLandValue", propertyPath, context);
                ValidateNullableInt(element, "CommitmentInterestID", propertyPath, context);
                ValidateNullableDateTime(element, "CommitmentEffectiveDate", propertyPath, context);
                ValidateNullableInt(element, "YearAcquired", propertyPath, context);

                // Validate strings
                ValidateString(element, "Leasehold", propertyPath, context, isRequired: false);
                ValidateString(element, "Legal", propertyPath, context, isRequired: false);
                ValidateString(element, "ParcelID", propertyPath, context, isRequired: false);
                ValidateString(element, "ProposedInsured", propertyPath, context, isRequired: false);
                ValidateString(element, "Vesting", propertyPath, context, isRequired: false);

                // Validate arrays using the 4-parameter version (with index)
                ValidateArrayPropertyWithIndex(element, "ChainOfTitle", propertyPath, context, ValidateChainOfTitleItem);
                ValidateArrayPropertyWithIndex(element, "Easements", propertyPath, context, ValidateEasementItem);
                ValidateArrayPropertyWithIndex(element, "Liens", propertyPath, context, ValidateLienItem);
                ValidateArrayPropertyWithIndex(element, "Requirements", propertyPath, context, ValidateRequirementItem);
                ValidateArrayPropertyWithIndex(element, "Restrictions", propertyPath, context, ValidateRestrictionItem);
                ValidateArrayPropertyWithIndex(element, "Taxes", propertyPath, context, ValidateTaxItem);
            }
            catch (JsonException ex)
            {
                context.AddFailure(propertyPath, $"Invalid JSON format: {ex.Message}");
            }
        }

        private void ValidateActionEventData(object actionEventData, ValidationContext<FlexibleOrderDto> context, string propertyPath)
        {
            try
            {
                var json = JsonSerializer.Serialize(actionEventData);
                var element = JsonSerializer.Deserialize<JsonElement>(json);

                // Validate required fields
                ValidateRequiredString(element, "FileNumber", propertyPath, context, isRequired: true);

                if (element.TryGetPropertyCaseInsensitive("ActionEventCode", out var codeElement, _jsonOptions))
                {
                    if (codeElement.ValueKind == JsonValueKind.String)
                    {
                        var code = codeElement.GetString();
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            context.AddFailure($"{propertyPath}.ActionEventCode", "ActionEventCode is required");
                        }
                    }
                    else
                    {
                        context.AddFailure($"{propertyPath}.ActionEventCode", "ActionEventCode must be a string");
                    }
                }
                else
                {
                    context.AddFailure($"{propertyPath}.ActionEventCode", "ActionEventCode is required");
                }
            }
            catch (JsonException ex)
            {
                context.AddFailure(propertyPath, $"Invalid JSON format: {ex.Message}");
            }
        }

        #region Helper Validation Methods

        private void ValidateRequiredString(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context, bool isRequired = true, int? maxLength = null)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                {
                    if (isRequired)
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} cannot be null");
                }
                else if (property.ValueKind != JsonValueKind.String)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a string");
                }
                else
                {
                    var value = property.GetString();
                    if (isRequired && string.IsNullOrWhiteSpace(value))
                    {
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} is required");
                    }
                    if (maxLength.HasValue && value?.Length > maxLength.Value)
                    {
                        context.AddFailure($"{parentPath}.{propertyName}",
                            $"{propertyName} cannot exceed {maxLength.Value} characters");
                    }
                }
            }
            else if (isRequired)
            {
                context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} is required");
            }
        }

        private void ValidateString(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context, bool isRequired = false, int? maxLength = null)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                {
                    if (isRequired)
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} cannot be null");
                }
                else if (property.ValueKind != JsonValueKind.String)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a string");
                }
                else if (maxLength.HasValue)
                {
                    var value = property.GetString();
                    if (value?.Length > maxLength.Value)
                    {
                        context.AddFailure($"{parentPath}.{propertyName}",
                            $"{propertyName} cannot exceed {maxLength.Value} characters");
                    }
                }
            }
            else if (isRequired)
            {
                context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} is required");
            }
        }

        private void ValidateNullableDecimal(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                    return;

                if (property.ValueKind == JsonValueKind.Number)
                {
                    try
                    {
                        property.GetDecimal();
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a valid decimal: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a decimal");
                }
            }
        }

        private void ValidateNullableInt(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                    return;

                if (property.ValueKind == JsonValueKind.Number)
                {
                    try
                    {
                        if (!property.TryGetInt32(out _))
                        {
                            context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be an integer");
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a valid integer: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be an integer");
                }
            }
        }

        private void ValidateNullableDateTime(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                    return;

                if (property.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        if (!DateTime.TryParse(property.GetString(), out _))
                        {
                            context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a valid date");
                        }
                    }
                    catch (Exception ex)
                    {
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a valid date: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a string date");
                }
            }
        }

        // Array validation for items that need index (4 parameters)
        private void ValidateArrayPropertyWithIndex(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context, Action<JsonElement, ValidationContext<FlexibleOrderDto>, string, int> itemValidator)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var arrayProperty, _jsonOptions))
            {
                if (arrayProperty.ValueKind == JsonValueKind.Array)
                {
                    int index = 0;
                    foreach (var item in arrayProperty.EnumerateArray())
                    {
                        itemValidator(item, context, $"{parentPath}.{propertyName}[{index}]", index);
                        index++;
                    }
                }
                else if (arrayProperty.ValueKind != JsonValueKind.Null)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be an array");
                }
            }
        }

        // Array validation for items that don't need index (3 parameters)
        private void ValidateArrayProperty(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context, Action<JsonElement, ValidationContext<FlexibleOrderDto>, string> itemValidator)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var arrayProperty, _jsonOptions))
            {
                if (arrayProperty.ValueKind == JsonValueKind.Array)
                {
                    int index = 0;
                    foreach (var item in arrayProperty.EnumerateArray())
                    {
                        itemValidator(item, context, $"{parentPath}.{propertyName}[{index}]");
                        index++;
                    }
                }
                else if (arrayProperty.ValueKind != JsonValueKind.Null)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be an array");
                }
            }
        }

        private void ValidateRequiredInt(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                try
                {
                    if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out _))
                    {
                        context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be an integer");
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a valid integer: {ex.Message}");
                }
            }
            else
            {
                context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} is required");
            }
        }

        private void ValidateNullableBool(JsonElement element, string propertyName, string parentPath,
            ValidationContext<FlexibleOrderDto> context)
        {
            if (element.TryGetPropertyCaseInsensitive(propertyName, out var property, _jsonOptions))
            {
                if (property.ValueKind == JsonValueKind.Null)
                    return;

                if (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
                {
                    context.AddFailure($"{parentPath}.{propertyName}", $"{propertyName} must be a boolean");
                }
            }
        }

        #endregion

        #region Item Validators for Arrays

        // 3-parameter validators (no index needed)
        private void ValidateBookPageItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path)
        {
            ValidateRequiredInt(item, "BookPageID", path, context);
            ValidateRequiredInt(item, "BookPageSourceTypeID", path, context);
            ValidateRequiredInt(item, "ReferenceID", path, context);
            ValidateRequiredInt(item, "SortOrder", path, context);
            ValidateString(item, "Book", path, context);
            ValidateString(item, "Page", path, context);
        }

        private void ValidateBookPageDataRowItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path)
        {
            ValidateRequiredInt(item, "BookPageID", path, context);
            ValidateRequiredInt(item, "BookPageSourceTypeID", path, context);
            ValidateRequiredInt(item, "ReferenceID", path, context);
            ValidateRequiredInt(item, "SortOrder", path, context);

            // Validate LienRequirementBase properties
            ValidateLienRequirementBaseProperties(item, context, path);
        }

        // 4-parameter validators (with index)
        private void ValidateChainOfTitleItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "ChainOfTitleID", path, context);
            ValidateNullableDecimal(item, "Consideration", path, context);
            ValidateNullableDateTime(item, "Dated", path, context);
            ValidateNullableDateTime(item, "Recorded", path, context);
            ValidateString(item, "DeedBookVolumePage", path, context);
            ValidateString(item, "DeedType", path, context);
            ValidateString(item, "Grantees", path, context);
            ValidateString(item, "Grantors", path, context);
            ValidateString(item, "Instrument", path, context);
            ValidateString(item, "Notes", path, context);

            // Validate BookPages array using 3-parameter validator
            ValidateArrayProperty(item, "BookPages", path, context, ValidateBookPageItem);

            // Validate ChainOfTitleBookPages array using 3-parameter validator
            ValidateArrayProperty(item, "ChainOfTitleBookPages", path, context, ValidateBookPageDataRowItem);
        }

        private void ValidateEasementItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "EasementID", path, context);
            ValidateNullableInt(item, "EasementTypeID", path, context);
            ValidateString(item, "EasementTypeName", path, context);

            // Validate EasementRestrictionBase properties
            ValidateEasementRestrictionBaseProperties(item, context, path);

            // Validate EasementBookPages array using 3-parameter validator
            ValidateArrayProperty(item, "EasementBookPages", path, context, ValidateBookPageDataRowItem);
        }

        private void ValidateLienItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "LienID", path, context);
            ValidateNullableInt(item, "LienTypeID", path, context);
            ValidateString(item, "LienTypeName", path, context);

            // Validate LienRequirementBase properties
            ValidateLienRequirementBaseProperties(item, context, path);

            // Validate LienBookPages array using 3-parameter validator
            ValidateArrayProperty(item, "LienBookPages", path, context, ValidateBookPageDataRowItem);
        }

        private void ValidateRequirementItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "RequirementID", path, context);
            ValidateNullableInt(item, "RequirementTypeID", path, context);
            ValidateString(item, "RequirementTypeName", path, context);

            // Validate LienRequirementBase properties
            ValidateLienRequirementBaseProperties(item, context, path);
        }

        private void ValidateRestrictionItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "RestrictionID", path, context);
            ValidateNullableInt(item, "RestrictionTypeID", path, context);
            ValidateString(item, "RestrictionTypeName", path, context);

            // Validate EasementRestrictionBase properties
            ValidateEasementRestrictionBaseProperties(item, context, path);
        }

        private void ValidateTaxItem(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path, int index)
        {
            ValidateNullableInt(item, "TaxID", path, context);
            ValidateNullableInt(item, "Year", path, context);
            ValidateNullableInt(item, "TaxTypeID", path, context);
            ValidateNullableInt(item, "PaymentFrequencyTypeID", path, context);
            ValidateNullableDecimal(item, "TotalAnnualTax", path, context);
            ValidateNullableDecimal(item, "Land", path, context);
            ValidateNullableDecimal(item, "Improvements", path, context);
            ValidateNullableDecimal(item, "Other", path, context);
            ValidateNullableDecimal(item, "FirstInstallment", path, context);
            ValidateNullableDecimal(item, "SecondInstallment", path, context);
            ValidateNullableDecimal(item, "ThirdInstallment", path, context);
            ValidateNullableDecimal(item, "FourthInstallment", path, context);
            ValidateNullableDecimal(item, "FirstPartiallyPaidAmount", path, context);
            ValidateNullableDecimal(item, "SecondPartiallyPaidAmount", path, context);
            ValidateNullableDecimal(item, "ThirdPartiallyPaidAmount", path, context);
            ValidateNullableDecimal(item, "FourthPartiallyPaidAmount", path, context);
            ValidateNullableDecimal(item, "ExemptionHomeowners", path, context);
            ValidateNullableDecimal(item, "ExemptionHomesteadSupplemental", path, context);
            ValidateNullableDecimal(item, "ExemptionMortgage", path, context);
            ValidateNullableDecimal(item, "ExemptionAdditional", path, context);
            ValidateNullableBool(item, "Estimated", path, context);
            ValidateNullableBool(item, "FirstDelinquent", path, context);
            ValidateNullableBool(item, "FirstDue", path, context);
            ValidateNullableBool(item, "FirstEstimated", path, context);
            ValidateNullableBool(item, "FirstPaid", path, context);
            ValidateNullableBool(item, "FirstPartiallyPaid", path, context);
            ValidateNullableBool(item, "SecondDelinquent", path, context);
            ValidateNullableBool(item, "SecondDue", path, context);
            ValidateNullableBool(item, "SecondEstimated", path, context);
            ValidateNullableBool(item, "SecondPaid", path, context);
            ValidateNullableBool(item, "SecondPartiallyPaid", path, context);
            ValidateNullableBool(item, "ThirdDelinquent", path, context);
            ValidateNullableBool(item, "ThirdDue", path, context);
            ValidateNullableBool(item, "ThirdEstimated", path, context);
            ValidateNullableBool(item, "ThirdPaid", path, context);
            ValidateNullableBool(item, "ThirdPartiallyPaid", path, context);
            ValidateNullableBool(item, "FourthDelinquent", path, context);
            ValidateNullableBool(item, "FourthDue", path, context);
            ValidateNullableBool(item, "FourthEstimated", path, context);
            ValidateNullableBool(item, "FourthPaid", path, context);
            ValidateNullableBool(item, "FourthPartiallyPaid", path, context);
            ValidateNullableDateTime(item, "FirstDelinquentDate", path, context);
            ValidateNullableDateTime(item, "FirstDiscountDate", path, context);
            ValidateNullableDateTime(item, "FirstDueDate", path, context);
            ValidateNullableDateTime(item, "FirstGoodThroughDate", path, context);
            ValidateNullableDateTime(item, "FirstPaidDate", path, context);
            ValidateNullableDateTime(item, "FirstTaxesOutDate", path, context);
            ValidateNullableDateTime(item, "SecondDelinquentDate", path, context);
            ValidateNullableDateTime(item, "SecondDiscountDate", path, context);
            ValidateNullableDateTime(item, "SecondDueDate", path, context);
            ValidateNullableDateTime(item, "SecondGoodThroughDate", path, context);
            ValidateNullableDateTime(item, "SecondPaidDate", path, context);
            ValidateNullableDateTime(item, "SecondTaxesOutDate", path, context);
            ValidateNullableDateTime(item, "ThirdDelinquentDate", path, context);
            ValidateNullableDateTime(item, "ThirdDiscountDate", path, context);
            ValidateNullableDateTime(item, "ThirdDueDate", path, context);
            ValidateNullableDateTime(item, "ThirdGoodThroughDate", path, context);
            ValidateNullableDateTime(item, "ThirdPaidDate", path, context);
            ValidateNullableDateTime(item, "ThirdTaxesOutDate", path, context);
            ValidateNullableDateTime(item, "FourthDelinquentDate", path, context);
            ValidateNullableDateTime(item, "FourthDiscountDate", path, context);
            ValidateNullableDateTime(item, "FourthDueDate", path, context);
            ValidateNullableDateTime(item, "FourthGoodThroughDate", path, context);
            ValidateNullableDateTime(item, "FourthPaidDate", path, context);
            ValidateNullableDateTime(item, "FourthTaxesOutDate", path, context);
            ValidateString(item, "Notes", path, context);
            ValidateString(item, "StateIDNumber", path, context);
            ValidateString(item, "TaxIDNumber", path, context);
            ValidateString(item, "TaxIDNumberFurtherDescribed", path, context);
            ValidateString(item, "TaxTypeName", path, context);
            ValidateString(item, "PaymentFrequencyTypeName", path, context);
            ValidateString(item, "TaxingEntity", path, context);
            ValidateString(item, "TaxingEntityCity", path, context);
            ValidateString(item, "TaxingEntityPhone", path, context);
            ValidateString(item, "TaxingEntityState", path, context);
            ValidateString(item, "TaxingEntityStreet1", path, context);
            ValidateString(item, "TaxingEntityStreet2", path, context);
            ValidateString(item, "TaxingEntityZipCode", path, context);
        }

        #endregion

        #region Base Property Validators

        private void ValidateLienRequirementBaseProperties(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path)
        {
            ValidateString(item, "Against", path, context);
            ValidateNullableDecimal(item, "Amount", path, context);
            ValidateString(item, "Assignee", path, context);
            ValidateString(item, "AssigneeBook", path, context);
            ValidateString(item, "AssigneeInstrument", path, context);
            ValidateString(item, "AssigneeLiber", path, context);
            ValidateString(item, "AssigneePage", path, context);
            ValidateString(item, "AssigneeVolume", path, context);
            ValidateString(item, "Assignor", path, context);
            ValidateString(item, "Book", path, context);
            ValidateString(item, "CaseNumber", path, context);
            ValidateString(item, "County", path, context);
            ValidateString(item, "CourtDistrict", path, context);
            ValidateString(item, "CourtType", path, context);
            ValidateString(item, "DocumentName", path, context);
            ValidateString(item, "Endorsements", path, context);
            ValidateString(item, "Grantee", path, context);
            ValidateString(item, "Grantor", path, context);
            ValidateString(item, "Holder", path, context);
            ValidateString(item, "InFavorOf", path, context);
            ValidateNullableDecimal(item, "InstallmentAmount", path, context);
            ValidateString(item, "InstallmentNumber", path, context);
            ValidateString(item, "Instrument", path, context);
            ValidateString(item, "Language", path, context);
            ValidateString(item, "Liber", path, context);
            ValidateString(item, "Page", path, context);
            ValidateString(item, "Purpose", path, context);
            ValidateString(item, "State", path, context);
            ValidateString(item, "StateDistrict", path, context);
            ValidateString(item, "TaxYears", path, context);
            ValidateString(item, "Trustee", path, context);
            ValidateString(item, "Volume", path, context);
            ValidateNullableDateTime(item, "Date", path, context);
            ValidateNullableDateTime(item, "RecordedDate", path, context);
            ValidateNullableDateTime(item, "MaturityDate", path, context);
            ValidateNullableBool(item, "Flagged", path, context);
            ValidateNullableBool(item, "IsAllCaps", path, context);

            // Validate BookPages array using 3-parameter validator
            ValidateArrayProperty(item, "BookPages", path, context, ValidateBookPageItem);
        }

        private void ValidateEasementRestrictionBaseProperties(JsonElement item, ValidationContext<FlexibleOrderDto> context, string path)
        {
            ValidateString(item, "Book", path, context);
            ValidateString(item, "DocumentName", path, context);
            ValidateString(item, "Grantee", path, context);
            ValidateString(item, "Grantor", path, context);
            ValidateString(item, "Instrument", path, context);
            ValidateString(item, "Language", path, context);
            ValidateString(item, "Liber", path, context);
            ValidateString(item, "Page", path, context);
            ValidateString(item, "Purpose", path, context);
            ValidateString(item, "Volume", path, context);
            ValidateNullableDateTime(item, "Date", path, context);
            ValidateNullableDateTime(item, "RecordedDate", path, context);
            ValidateNullableBool(item, "IsAllCaps", path, context);

            // Validate BookPages array using 3-parameter validator
            ValidateArrayProperty(item, "BookPages", path, context, ValidateBookPageItem);
        }

        #endregion

        #region Documents Array Validator

        private void ValidateDocumentsArray(JsonElement documents, ValidationContext<FlexibleOrderDto> context, string path)
        {
            if (documents.ValueKind != JsonValueKind.Array)
            {
                context.AddFailure(path, "Documents must be an array");
                return;
            }

            int docIndex = 0;
            foreach (var doc in documents.EnumerateArray())
            {
                var docPath = $"{path}[{docIndex}]";

                // Validate DocumentTypeID is required integer
                if (doc.TryGetPropertyCaseInsensitive("DocumentTypeID", out var docTypeId, _jsonOptions))
                {
                    try
                    {
                        if (!docTypeId.TryGetInt32(out _))
                        {
                            context.AddFailure($"{docPath}.DocumentTypeID", "DocumentTypeID must be an integer");
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is FormatException)
                    {
                        context.AddFailure($"{docPath}.DocumentTypeID", $"DocumentTypeID must be a valid integer: {ex.Message}");
                    }
                }
                else
                {
                    context.AddFailure($"{docPath}.DocumentTypeID", "DocumentTypeID is required");
                }

                ValidateString(doc, "FileName", docPath, context, isRequired: false, maxLength: 255);
                ValidateString(doc, "Description", docPath, context, isRequired: false, maxLength: 500);
                ValidateString(doc, "SourceFile", docPath, context, isRequired: false, maxLength: 500);
                ValidateNullableBool(doc, "InternalOnly", docPath, context);

                docIndex++;
            }
        }

        #endregion
    }
}
