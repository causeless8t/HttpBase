using System;

namespace Causeless3t.Network.Protocol
{
    [Serializable]
    public class SampleReq : BaseRequest
    {
        public int SamepleParam;
    }

    [Serializable]
    public class SampleAns : BaseResponse
    {
        
    }
}