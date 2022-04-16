// https://gist.github.com/ikriz/b0f9d96205629e19859e
using UnityEngine;

namespace Niantic.ARDK.External {

  public class EnumFlagAttribute : PropertyAttribute {
    public string enumName;

    public EnumFlagAttribute() { }

    public EnumFlagAttribute(string name) {
      enumName = name;
    }
  }

}