using System;

namespace NgLocalizer
{
    public class CodeHasChangedException : Exception
    {
        public CodeHasChangedException(string message) : base(message)
        {
            
        }
    }
}
