using System;
using System.Collections.Generic;
using System.Text;

namespace Wasl.Application.Interfaces.Common
{
    public interface ICurrentUserService
    {
        string? UserId();
    }
}
