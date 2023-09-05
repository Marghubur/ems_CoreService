namespace ModalLayer.Modal
{
    public class ResponseModel<T>
    {
        public string ErroMessage { set; get; } = null;
        public T Result { set; get; }
    }
}
