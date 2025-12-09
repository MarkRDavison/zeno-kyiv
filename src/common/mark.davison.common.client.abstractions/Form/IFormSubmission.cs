namespace mark.davison.common.client.abstractions.Form;

public interface IFormSubmission<TFormViewModel> where TFormViewModel : IFormViewModel
{
    Task<Response> Primary(TFormViewModel formViewModel);
}
