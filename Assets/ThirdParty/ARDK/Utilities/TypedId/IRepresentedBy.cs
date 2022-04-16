// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.Utilities.TypedId {
  /// <summary>
  /// Generic interface to mark a class as representable by an identifier of type TId.
  /// @note This interface is deliberately left empty (does not provide an accessor for TId) as
  ///   the identifier for implementing classes should be of type TypedId.
  /// </summary>
  /// <typeparam name="TId">Type of identifier that can be used to represent this class</typeparam>
  public interface IRepresentedBy<TId> 
  where
    TId: IEquatable<TId> 
  {  
  }
}