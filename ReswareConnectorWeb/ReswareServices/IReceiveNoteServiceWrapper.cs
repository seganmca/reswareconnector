using ReceiveNoteServiceNS;

namespace ReswareConnectorWeb.ReswareServices
{
    public interface IReceiveNoteServiceWrapper : IDisposable
    {
        Task<ReceiveNoteResponse> ReceiveNoteAsync(ReceiveNoteData noteData);
    }
}
