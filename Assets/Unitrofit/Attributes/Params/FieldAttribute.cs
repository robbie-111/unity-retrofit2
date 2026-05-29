using System;

namespace Unitrofit.Attributes.Params
{
    /// <summary>
    /// application/x-www-form-urlencoded 필드.
    /// [Post("/login")] void Login([Field("user")] string u, [Field("pw")] string pw)
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class FieldAttribute : Attribute
    {
        public string Name { get; }
        public FieldAttribute(string name = null) { Name = name; }
    }
}
