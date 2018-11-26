using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.API.Utility;
using System.Security.Cryptography;
using MPS.API.SDK.Parser;

namespace MPS.API.SDK.DangDang
{
    /// <summary>
    /// 当当客户端。
    /// </summary>
    public class DopClient : IClient
    {
        private string _shopId;
        private string _apiKey;
        private string _appSecret;
        private string _sessionKey;
        private string _appUrl;

        public DopClient(string shopId, string apiKey,string sessionKey,string appUrl,string appSecret)
        {
            if (apiKey == null) throw new ArgumentNullException("无ApiKey信息", "ApiKey");

            this._shopId = shopId;
            this._apiKey = apiKey;
            this._sessionKey = sessionKey;
            this._appUrl = appUrl;
            this._appSecret = appSecret;
        }

        public T Execute<T>(IRequest<T> request, string session) where T : IResponse
        {
            return Execute(request);
        }

        /// <summary>
        /// 执行公开API请求。
        /// </summary>
        /// <typeparam name="T">领域对象</typeparam>
        /// <param name="request">具体的API请求</param>
        /// <returns>领域对象</returns>
        public T Execute<T>(IRequest<T> request) where T : IResponse
        {
            // 添加系统级请求参数
            SdkDictionary txtParams = new SdkDictionary(request.GetParameters());

            txtParams.Add("method", request.GetApiNameOrUrl());
            txtParams.Add("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            txtParams.Add("format", "xml");
            txtParams.Add("app_key", this._apiKey);
            txtParams.Add("v", "1.0");
            txtParams.Add("sign_method", "md5");
            txtParams.Add("session", this._sessionKey);

            // 添加签名参数
            SdkDictionary signParams = new SdkDictionary();
            signParams.Add("app_key", _apiKey);
            signParams.Add("format", "xml");
            signParams.Add("method", request.GetApiNameOrUrl());
            signParams.Add("session", this._sessionKey);
            signParams.Add("sign_method","md5");
            signParams.Add("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            //signParams.Add("secret", _appSecret);
            signParams.Add("v", "1.0");

            txtParams.Add("sign", SignRequest(signParams, _appSecret));

            // 是否需要上传文件
            string body;
            WebUtils webUtils = new WebUtils();
            webUtils.Charset = "GBK";
            
            if (request is IUploadRequest<T>)
            {
                IUploadRequest<T> uRequest = (IUploadRequest<T>)request;
                IDictionary<string, FileItem> fileParams = Utils.CleanupDictionary(uRequest.GetFileParameters());
                body = webUtils.DoPost(_appUrl, txtParams, fileParams);
            }
            else
            {
                body = webUtils.DoGet(_appUrl, txtParams);
            }

            T rsp;

            IParser<T> tp = new XmlParser<T>();
            rsp = tp.Parse(body, Encoding.GetEncoding("GBK"));

            return rsp;

        }

        private object SignRequest(SdkDictionary parameters, string apiKey)
        {
            // 第一步：把字典按Key的字母顺序排序
            IDictionary<string, string> sortedParams = new SortedDictionary<string, string>(parameters);
            IEnumerator<KeyValuePair<string, string>> dem = sortedParams.GetEnumerator();

            // 第二步：把所有参数名和参数值串在一起 ：将AppSecret拼接到参数字符串头和尾
            StringBuilder query = new StringBuilder();

            query.Append(apiKey);

            while (dem.MoveNext())
            {
                string key = dem.Current.Key;
                string value = dem.Current.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    query.Append(key);
                    query.Append(value);
                }
            }

            query.Append(apiKey);

            // 第三步：使用MD5加密
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.GetEncoding("GBK").GetBytes(query.ToString());
            byte[] md5Data = md5.ComputeHash(data);
            md5.Clear();
            return BitConverter.ToString(md5Data).Replace("-", "").ToUpper();           
        }

    }
}
