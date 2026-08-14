using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class ForbiddenOperationException: DomainException
    {
        public ForbiddenOperationException(string operationDescription)
        : base($"Недостаточно прав для выполнения операции: {operationDescription}.") { }
    }
}
