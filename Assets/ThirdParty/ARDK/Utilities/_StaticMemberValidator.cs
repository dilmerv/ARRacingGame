// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Niantic.ARDK.Utilities
{
  internal static class _StaticMemberValidator
  {
#if MUST_VALIDATE_STATIC_MEMBERS
    private static readonly ConcurrentDictionary<FieldInfo, object> _fieldsToValidate =
      new ConcurrentDictionary<FieldInfo, object>();

    // Each field in _collectionsToValidate is an IEnumerable that will be validated as being empty.
    // If they are not empty they will be cleared.
    private static readonly object _collectionsToValidateLock = new object();
    private static readonly HashSet<FieldInfo> _collectionsToValidate = new HashSet<FieldInfo>();

    // Each field in _collectionArrayssToValidate is an Array of IEnumerables that will be validated
    // as being composed of only empty IEnumerables.
    private static readonly object _collectionArraysToValidateLock = new object();
    private static readonly HashSet<FieldInfo> _collectionArraysToValidate = new HashSet<FieldInfo>();

    // Do not call this method directly. Use a _StaticMembersValidatorScope instead.
    internal static void _ForScopeOnly_CheckCleanState()
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(_StaticMembersValidatorScope));

      try
      {
        // Compare each field to validate to its expected value, and if any are different throw.
        foreach (var pair in _fieldsToValidate)
        {
          var field = pair.Key;
          var expectedValue = pair.Value;

          var value = field.GetValue(null);

          if (!object.Equals(value, expectedValue))
          {
            string message =
              "Field '" + field.DeclaringType.FullName + "." + field.Name +"' contains garbage.\n" +
              "Expected value: " + expectedValue + ".\n" +
              "Actual value: " + value + ".";

            throw new InvalidOperationException(message);
          }
        }

        _fieldsToValidate.Clear();

        // Check that each collection is empty, and if any contain values then throw.
        lock (_collectionsToValidateLock)
        {
          foreach (var field in _collectionsToValidate)
          {
            var untypedValue = field.GetValue(null);

            if (untypedValue == null)
              continue;

            var collection = (IEnumerable)untypedValue;
            foreach (var value in collection)
            {
              string message =
                "Collection '" + field.DeclaringType.FullName + "." + field.Name + "' is not empty.";
              
              throw new InvalidOperationException(message);
            }
          }

          _collectionsToValidate.Clear();
        }

        // Iterate through each array to validate, and if any collections from any arrays contain
        // values then throw.
        lock (_collectionArraysToValidateLock)
        {
          foreach (var field in _collectionArraysToValidate)
          {
            var untypedValue = field.GetValue(null);

            if (untypedValue == null)
              continue;

            var array = (ICollection)untypedValue;
            int index = 0;
            foreach (var subcollection in array)
            {
              var collection = (IEnumerable)subcollection;
              foreach (var value in collection)
              {
                string message =
                  "Collection '" + field.DeclaringType.FullName + "." + field.Name + "[" + index +
                  "]" + "' is not empty.";
                
                throw new InvalidOperationException(message);
              }

              index++;
            }
          }

          _collectionArraysToValidate.Clear();
        }
      } 
      catch
      {
        // If any test fails, we just nullify/restore all fields and clear all collections.
        foreach(var pair in _fieldsToValidate)
        {
          var field = pair.Key;
          var value = pair.Value;

          field.SetValue(null, value);
        }

        _fieldsToValidate.Clear();

        lock (_collectionsToValidateLock)
        {
          foreach (var fieldCollection in _collectionsToValidate)
          {
            var untypedCollection = fieldCollection.GetValue(null);
            if (untypedCollection == null)
              continue;

            var clearMethod =
              untypedCollection.GetType()
                .GetMethod
                (
                  "Clear",
                  BindingFlags.Public | BindingFlags.Instance,
                  null,
                  Type.EmptyTypes,
                  null
                );

            if (clearMethod != null)
              clearMethod.Invoke(untypedCollection, null);
          }

          _collectionsToValidate.Clear();
        }

        lock (_collectionArraysToValidateLock)
        {
          foreach(var fieldCollection in _collectionArraysToValidate)
          {
            var untypedArray = fieldCollection.GetValue(null);
            if (untypedArray == null)
              continue;
            
            var array = (ICollection)untypedArray;
            int index = 0;
            foreach (var untypedCollection in array)
            {
              var clearMethod =
                untypedCollection.GetType()
                .GetMethod
                (
                  "Clear",
                  BindingFlags.Public | BindingFlags.Instance,
                  null,
                  Type.EmptyTypes,
                  null
                );

              if (clearMethod != null)
                clearMethod.Invoke(untypedCollection, null);
            }
          }

          _collectionArraysToValidate.Clear();
        }
        
        throw;
      }
    }
#endif
    
    // Use only with static fields.
    // Usage like: _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => staticField);
    [Conditional("MUST_VALIDATE_STATIC_MEMBERS")]
    internal static void _FieldIsNullWhenScopeEnds<T>(Expression<Func<T>> expression)
      where T: class
    {
      _FieldContainsSpecificValueWhenScopeEnds(expression, null);
    }

    // Use only with static fields.
    // Usage like:
    //   _StaticMemberValidator._FieldContainsSpecificValueWhenScopeEnds(() => staticField, 123);
    [Conditional("MUST_VALIDATE_STATIC_MEMBERS")]
    internal static void _FieldContainsSpecificValueWhenScopeEnds<T>
    (
      Expression<Func<T>> expression,
      T valueAtEnd
    )
    {
#if MUST_VALIDATE_STATIC_MEMBERS
      var memberExpression = (MemberExpression)expression.Body;
      var field = (FieldInfo)memberExpression.Member;

      if (!field.IsStatic)
        throw new InvalidOperationException("Given field is not static.");

      if (!_fieldsToValidate.TryAdd(field, valueAtEnd))
      {
        object existingValue;
        _fieldsToValidate.TryGetValue(field, out existingValue);

        if (!object.Equals(existingValue, valueAtEnd))
        {
          string message =
            "The field " + field.DeclaringType.FullName + "." + field.Name +
            " already has a valueAtEnd expectation to be '" + existingValue +
            "' but we are now trying to set a new expectation of '" + valueAtEnd + "'.";

          throw new InvalidOperationException(message);
        }
      }

      _StaticMembersValidatorScope._ForValidatorOnly_CheckScopeExists();
#endif
    }
    
    // Use only with static collection fields.
    // Usage like:
    //   _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => staticCollectionField);
    [Conditional("MUST_VALIDATE_STATIC_MEMBERS")]
    internal static void _CollectionIsEmptyWhenScopeEnds(Expression<Func<IEnumerable>> expression)
    {
#if MUST_VALIDATE_STATIC_MEMBERS
      var memberExpression = (MemberExpression)expression.Body;
      var field = (FieldInfo)memberExpression.Member;
      
      if (!field.IsStatic)
        throw new InvalidOperationException("Given field is not static.");

      lock (_collectionsToValidateLock)
        if (!_collectionsToValidate.Contains(field))
          _collectionsToValidate.Add(field);
      
      _StaticMembersValidatorScope._ForValidatorOnly_CheckScopeExists();
#endif
    }
    
    // Use only with static fields that are arrays of collections.
    // Usage like:
    //   _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => staticArrayOfCollectionsField);
    // Where staticArrayOfCollectionField looks like:
    //   static ICollection<TValue>[] staticArrayCollectionField;
    [Conditional("MUST_VALIDATE_STATIC_MEMBERS")]
    internal static void _CollectionsAreEmptyWhenScopeEnds(Expression<Func<Array>> expression)
    {
#if MUST_VALIDATE_STATIC_MEMBERS
      var memberExpression = (MemberExpression)expression.Body;
      var field = (FieldInfo)memberExpression.Member;
      
      if (!field.IsStatic)
        throw new InvalidOperationException("Given field is not static.");

      lock (_collectionArraysToValidateLock)
        if (!_collectionArraysToValidate.Contains(field))
          _collectionArraysToValidate.Add(field);
      
      _StaticMembersValidatorScope._ForValidatorOnly_CheckScopeExists();
#endif
    }
  }
}
