namespace Unitrofit.Core
{
    /// <summary>
    /// Retrofit4Unity 호환 콜백 패턴.
    /// 인터페이스 메서드의 첫 번째 파라미터로 사용한다.
    /// </summary>
    public class Callback<T>
    {
        public delegate void SuccessDelegate(T response);
        public delegate void ErrorDelegate(UnitrofitException error);

        public SuccessDelegate OnSuccess { get; set; }
        public ErrorDelegate   OnError   { get; set; }

        public Callback(SuccessDelegate onSuccess, ErrorDelegate onError = null)
        {
            OnSuccess = onSuccess;
            OnError   = onError;
        }
    }
}
