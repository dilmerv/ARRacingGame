using System;


namespace ARFoundationRemote.Editor {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ShowInInspectorAttribute : Attribute {
    }
}
