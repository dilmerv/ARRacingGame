// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Diagnostics;

namespace Niantic.ARDK.Utilities
{
  // In .NET we don't have a "friend class" modifier or similar.
  // The best we can do, by default, is to use internal. Yet, internal is still "too open".
  // Sometimes we create a Factory and we want every call to pass through the factory (as it has
  // additional steps after the constructor runs). Yet, a test, or any other class in the project
  // can just "run" any internal code without passing through the factory.
  // Well, this class tries to solve this problem by allowing us to validate who is the caller to 
  // our method. For performance considerations, this code only runs in MUST_VALIDATE_STATIC_MEMBERS
  // builds, though and, unfortunately, will not capture bad calls at compile-time, just at
  // run-time.
  internal static class _FriendTypeAsserter
  {
    [Conditional("MUST_VALIDATE_STATIC_MEMBERS")]
    public static void AssertCallerIs(Type type)
    {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      
      // 0 = this method.
      // 1 = the method that wants to validate its caller.
      var callerFrame = new StackFrame(1);
      
      var callerMethod = callerFrame.GetMethod();

      bool isCallerSomehowInternal =
        callerMethod.IsAssembly ||
        callerMethod.IsFamilyAndAssembly ||
        callerMethod.IsFamilyOrAssembly;

      if (!isCallerSomehowInternal)
        throw new InvalidOperationException("Friend Type asserts are for internal methods only.");

      // 2 = the method to be validated.
      var frameToValidate = new StackFrame(2);

      var methodToValidate = frameToValidate.GetMethod();
      if (methodToValidate.DeclaringType != type)
        throw new InvalidOperationException("The declaring type is: " + methodToValidate.DeclaringType +
            " but this method can only be invoked by: " + type.FullName);
    }
  }
}
