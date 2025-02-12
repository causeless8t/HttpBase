using Causeless3t.Network.Protocol;

namespace Causeless3t.Network
{
    [API("sample/test")]
    public class SampleHandler : RequestHandler<SampleReq, SampleAns, int>
    {
        protected override SampleReq MakeReqMessage(int param) => new()
        {
            SamepleParam = param
        };

        protected override void Process(RequestInfo requestInfo, SampleAns response)
        {
            
        }

        public override void ErrorProcess(RequestInfo requestInfo, BaseResponse response, int error)
        {
            
        }
    }
}

