namespace mark.davison.common.client.abstractions.Form;

public interface IForm<TFormViewModel> where TFormViewModel : IFormViewModel
{
    TFormViewModel FormViewModel { get; set; }
}