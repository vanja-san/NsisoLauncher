﻿using System;
using System.Threading.Tasks;
using NsisoLauncher.Core.Net.MojangApi.Api;
using Newtonsoft.Json.Linq;
using NsisoLauncher.Core.Net.MojangApi.Responses;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using static NsisoLauncher.Core.Net.MojangApi.Responses.ChallengesResponse;

namespace NsisoLauncher.Core.Net.MojangApi.Endpoints
{

    /// <summary>
    /// 验证问题请求类
    /// </summary>
    public class Challenges : IEndpoint<ChallengesResponse>
    {

        /// <summary>
        /// 实例化允许用户所要回答的验证问题的endpoint。
        /// </summary>
        /// <param name="token">有效的用户令牌</param>
        public Challenges(string accessToken)
        {
            this.Address = new Uri("https://api.mojang.com/user/security/challenges");
            this.Arguments.Add(accessToken);
        }

        /// <summary>
        /// 执行请求并返回响应属性
        /// </summary>
        public override async Task<ChallengesResponse> PerformRequestAsync()
        {
            this.Response = await Requester.Get(this, true);

            if (this.Response.IsSuccess)
            {
                JArray jchallenges = JArray.Parse(this.Response.RawMessage);
                List<Challenge> challenges = new List<Challenge>();
                foreach (JToken token in jchallenges)
                    challenges.Add(ChallengesResponse.Parse(token));

                return new ChallengesResponse(this.Response)
                {
                    Challenges = challenges
                };
            }
            else
                return new ChallengesResponse(Error.GetError(this.Response));
        }
    }
}
