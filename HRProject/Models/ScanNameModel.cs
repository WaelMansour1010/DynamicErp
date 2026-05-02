namespace EazyCash.Models.ScanName
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

            [JsonProperty("ns0:SafewatchResponse_elm")]
            public Ns0SafewatchResponseElm ns0SafewatchResponse_elm { get; set; }
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

        public class Ns0ResponseData
        {
            [JsonProperty("ns0:scanModel")]
            public Ns0ScanModel ns0scanModel { get; set; }
        }

        public class Ns0ResponseStatus
        {
            [JsonProperty("ns0:Code")]
            public string ns0Code { get; set; }

            [JsonProperty("ns0:Description")]
            public string ns0Description { get; set; }
        }

        public class Ns0SafewatchResponseElm
        {
            [JsonProperty("@xmlns:cd")]
            public string xmlnscd { get; set; }

            [JsonProperty("@xmlns:ns0")]
            public string xmlnsns0 { get; set; }

            [JsonProperty("cal:CallingData")]
            public CalCallingData calCallingData { get; set; }

            [JsonProperty("ns0:ResponseStatus")]
            public Ns0ResponseStatus ns0ResponseStatus { get; set; }

            [JsonProperty("ns0:ResponseData")]
            public Ns0ResponseData ns0ResponseData { get; set; }
        }

        public class Ns0ScanModel
        {
            [JsonProperty("ns0:detecId")]
            public string ns0detecId { get; set; }

            [JsonProperty("ns0:violation")]
            public string ns0violation { get; set; }
        }

        public class ScanNameRoot
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
