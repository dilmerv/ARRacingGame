using System;
using System.Reflection;

namespace FlatBuffers {

  internal static class TableFieldLookup<T> {
    public static FieldInfo fieldInfo;
    public static object lockable = new object();
  }

  public static class TableUtils {
    public static Table GetTable<T>(this T table)  where T : IFlatbufferObject{
      var fieldInfo = TableFieldLookup<T>.fieldInfo;
      if (fieldInfo == null) {
        lock (TableFieldLookup<T>.lockable) {
          if (fieldInfo == null) {
            fieldInfo = typeof(T).GetField("__p", BindingFlags.NonPublic | BindingFlags.Instance);
            TableFieldLookup<T>.fieldInfo = fieldInfo;
          }
        }
      }

      return (Table) fieldInfo.GetValue(table);
    }
  }

}
