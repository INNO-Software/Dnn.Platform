﻿using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using DotNetNuke.Entities.Controllers;

namespace OAuth.AuthorizationServer.Core.Server
{
    // Responsible for providing the authorization server key to use in signing the token
    public class AuthorizationServerSigningKeyManager
    {
        public RSACryptoServiceProvider GetSigner()
        {
            var signer = new RSACryptoServiceProvider();
            var base64EncodedKey = OAUTHDataController.GetSettings().AuthorizationServerPrivateKey; 
            signer.FromXmlString(Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedKey)));
            return signer;            
        }
    }
}
