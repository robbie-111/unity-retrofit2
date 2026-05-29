using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// multipart/form-data 파트. [Multipart]와 함께 사용.
    /// MultipartBody 타입 파라미터에 적용한다.
    /// [Multipart][Post("/upload")] void Upload([Part] MultipartBody file)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class PartAttribute : Attribute
    {
        public string Name { get; }
        public PartAttribute(string name = null) { Name = name; }
    }
}
