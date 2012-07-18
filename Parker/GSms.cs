/*
 *  
 * GSms - Partial implementation of Globe SMS API
 * Author: Ruel Pagayon
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace GSms
{
    public class SmsSender
    {
        public String uName;
        public String uPin;
        public String Number;
        public String Message;
        public String Disp;
        public String Udh;
        public String Mwi;
        public String Coding;

        public SmsSender(String name, String pin)
        {
            uName = name;
            uPin = pin;
            Disp = "1";
            Udh = "";
            Mwi = "";
            Coding = "0";
        }

        public String SendSms(String target, String msg)
        {
            Number = target;
            Message = msg;

            String resStr;
            String url = "http://iplaypen.globelabs.com.ph:1881/axis2/services/Platform/sendSMS";
            String qStr = "?uName=" + uName +
                            "&uPin=" + uPin +
                            "&MSISDN=" + Number +
                            "&messageString=" + Message +
                            "&Display=" + Disp +
                            "&udh=" + Udh +
                            "&mwi=" + Mwi +
                            "&coding=" + Coding;
            HttpWebRequest request = (HttpWebRequest)
                        WebRequest.Create(url + qStr);
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            resStr = reader.ReadToEnd();
            return resStr;
        }
    }
}
