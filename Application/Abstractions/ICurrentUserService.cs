using System;

namespace Habitu.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}