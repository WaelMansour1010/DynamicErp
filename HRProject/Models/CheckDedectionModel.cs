namespace EazyCash.Models.CheckDedection
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class CalCallingData
        {
            [JsonProperty("@xmlns:cal")]
            public string xmlnscal { get; set; }

            [JsonProperty("cal:ChannelId")]
            public string calChannelId { get; set; }

            [JsonProperty("cal:RequestId")]
            public string calRequestId { get; set; }
        }

        public class EnvBody
        {
            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("tns:CheckDetectionResponse_elm")]
            public TnsCheckDetectionResponseElm tnsCheckDetectionResponse_elm { get; set; }
        }

        public class EnvHeader
        {
            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("wsa:Action")]
            public WsaAction wsaAction { get; set; }

            [JsonProperty("wsa:MessageID")]
            public WsaMessageID wsaMessageID { get; set; }

            [JsonProperty("wsa:ReplyTo")]
            public WsaReplyTo wsaReplyTo { get; set; }

            [JsonProperty("wsa:FaultTo")]
            public WsaFaultTo wsaFaultTo { get; set; }
        }

        public class InstraTrackingCorrelationFlowId
        {
            [JsonProperty("@xmlns:instra")]
            public string xmlnsinstra { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class InstraTrackingEcid
        {
            [JsonProperty("@xmlns:instra")]
            public string xmlnsinstra { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class InstraTrackingFlowEventId
        {
            [JsonProperty("@xmlns:instra")]
            public string xmlnsinstra { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class InstraTrackingFlowId
        {
            [JsonProperty("@xmlns:instra")]
            public string xmlnsinstra { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class InstraTrackingQuiescingSCAEntityId
        {
            [JsonProperty("@xmlns:instra")]
            public string xmlnsinstra { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class CheckDedectionsRoot
        {
            [JsonProperty("soapenv:Envelope")]
            public SoapenvEnvelope soapenvEnvelope { get; set; }
        }

        public class SoapenvEnvelope
        {
            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("@xmlns:soapenv")]
            public string xmlnssoapenv { get; set; }

            [JsonProperty("env:Header")]
            public EnvHeader envHeader { get; set; }

            [JsonProperty("env:Body")]
            public EnvBody envBody { get; set; }
        }

        public class TnsCheckDetectionResponseElm
        {
            [JsonProperty("@xmlns:wsp1_2")]
            public string xmlnswsp1_2 { get; set; }

            [JsonProperty("@xmlns:plnk")]
            public string xmlnsplnk { get; set; }

            [JsonProperty("@xmlns:wsdl")]
            public string xmlnswsdl { get; set; }

            [JsonProperty("@xmlns:wsp")]
            public string xmlnswsp { get; set; }

            [JsonProperty("@xmlns:wsu")]
            public string xmlnswsu { get; set; }

            [JsonProperty("@xmlns:client")]
            public string xmlnsclient { get; set; }

            [JsonProperty("@xmlns:soap")]
            public string xmlnssoap { get; set; }

            [JsonProperty("@xmlns:wsam")]
            public string xmlnswsam { get; set; }

            [JsonProperty("@xmlns:tns")]
            public string xmlnstns { get; set; }

            [JsonProperty("@xmlns")]
            public string xmlns { get; set; }

            [JsonProperty("cal:CallingData")]
            public CalCallingData calCallingData { get; set; }

            [JsonProperty("tns:ResponseStatus")]
            public TnsResponseStatus tnsResponseStatus { get; set; }

            [JsonProperty("tns:ResponseData")]
            public TnsResponseData tnsResponseData { get; set; }
        }

        public class TnsResponseData
        {
            [JsonProperty("tns:return")]
            public TnsReturn tnsreturn { get; set; }
        }

        public class TnsResponseStatus
        {
            [JsonProperty("tns:Code")]
            public string tnsCode { get; set; }

            [JsonProperty("tns:Description")]
            public string tnsDescription { get; set; }
        }

        public class TnsReturn
        {
            [JsonProperty("tns:detectionId")]
            public string tnsdetectionId { get; set; }
        }

        public class WsaAction
        {
            [JsonProperty("@xmlns:soapenv")]
            public string xmlnssoapenv { get; set; }

            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class WsaFaultTo
        {
            [JsonProperty("@xmlns:soapenv")]
            public string xmlnssoapenv { get; set; }

            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("wsa:Address")]
            public string wsaAddress { get; set; }
        }

        public class WsaMessageID
        {
            [JsonProperty("@xmlns:soapenv")]
            public string xmlnssoapenv { get; set; }

            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("#text")]
            public string text { get; set; }
        }

        public class WsaReferenceParameters
        {
            [JsonProperty("instra:tracking.ecid")]
            public InstraTrackingEcid instratrackingecid { get; set; }

            [JsonProperty("instra:tracking.FlowEventId")]
            public InstraTrackingFlowEventId instratrackingFlowEventId { get; set; }

            [JsonProperty("instra:tracking.FlowId")]
            public InstraTrackingFlowId instratrackingFlowId { get; set; }

            [JsonProperty("instra:tracking.CorrelationFlowId")]
            public InstraTrackingCorrelationFlowId instratrackingCorrelationFlowId { get; set; }

            [JsonProperty("instra:tracking.quiescing.SCAEntityId")]
            public InstraTrackingQuiescingSCAEntityId instratrackingquiescingSCAEntityId { get; set; }
        }

        public class WsaReplyTo
        {
            [JsonProperty("@xmlns:soapenv")]
            public string xmlnssoapenv { get; set; }

            [JsonProperty("@xmlns:wsa")]
            public string xmlnswsa { get; set; }

            [JsonProperty("@xmlns:env")]
            public string xmlnsenv { get; set; }

            [JsonProperty("wsa:Address")]
            public string wsaAddress { get; set; }

            [JsonProperty("wsa:ReferenceParameters")]
            public WsaReferenceParameters wsaReferenceParameters { get; set; }
        }


    

}
