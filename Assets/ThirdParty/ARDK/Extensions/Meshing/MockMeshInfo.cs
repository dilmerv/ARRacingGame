using System;

namespace Niantic.ARDK.Extensions.Meshing
{
    [Serializable]
    public class MockMeshInfo
    {
        public string deviceModel;
        public string dateTime;
        public string ardkVersion;
        public int vertices;
        public int faces;

        public MockMeshInfo(string deviceModel, string dateTime, string ardkVersion)
        {
            this.deviceModel = deviceModel;
            this.dateTime = dateTime;
            this.ardkVersion = ardkVersion;
        }
    }
}