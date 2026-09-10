using System;
namespace GameMaster.Core.Exceptions;

public class InsufficientPointsException : Exception {
    public InsufficientPointsException(string message) : base(message) {}
}
public class PermissionDeniedException : Exception {
    public PermissionDeniedException(string message) : base(message) {}
}
public class AppNotFoundException : Exception {
    public AppNotFoundException(string message) : base(message) {}
}
public class DeviceNotFoundException : Exception {
    public DeviceNotFoundException(string message) : base(message) {}
}
