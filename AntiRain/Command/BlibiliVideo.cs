﻿using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BilibiliApi.Video;
using BilibiliApi.Video.Models;
using JetBrains.Annotations;
using Sora.Attributes.Command;
using Sora.Entities;
using Sora.Entities.MessageElement;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.FormatLog;

namespace AntiRain.Command
{
    /// <summary>
    /// BV/AV号解析
    /// </summary>
    [CommandGroup]
    public static class BlibiliVideo
    {
        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] { @"(?:BV|bv|AV|av)[a-zA-Z0-9]+" },
                      MatchType = MatchType.Regex)]
        public static async ValueTask VideoInfoById(GroupMessageEventArgs eventArgs)
        {
            Regex idRegex    = new(@"(?:BV|bv|AV|av)[a-zA-Z0-9]+");
            var   videoIdStr = idRegex.Match(eventArgs.Message.RawText).Value;
            var   videoInfo  = VideoApis.GetVideoInfo(videoIdStr);
            if (videoInfo.Code != 0)
            {
                await eventArgs.Reply($"API发生错误({videoInfo.Code})\r\nmessage:{videoInfo.Message}");
                return;
            }

            //发送视频信息
            await eventArgs.Reply(GenReplyMessage(videoInfo));
        }

        [UsedImplicitly]
        [GroupCommand(CommandExpressions = new[] { @"https://b23\.tv/[a-zA-Z0-9]+" },
                      MatchType = MatchType.Regex,
                      Priority = 0)]
        public static async ValueTask VideoInfoByMiniApp(GroupMessageEventArgs eventArgs)
        {
            //获取短链
            Regex urlRegex    = new(@"https://b23\.tv/[a-zA-Z0-9]+");
            var   videoUrlStr = urlRegex.Match(eventArgs.Message.RawText).Value;
            if (string.IsNullOrEmpty(videoUrlStr)) return;
            //网络请求获取跳转地址
            var handler  = new HttpClientHandler { AllowAutoRedirect = false };
            var client   = new HttpClient(handler);
            var response = await client.GetAsync(videoUrlStr);
            //解析id
            Regex idRegex    = new(@"(?:BV|bv|AV|av)[a-zA-Z0-9]+");
            var   videoIdStr = idRegex.Match(response.Headers.Location?.ToString() ?? string.Empty).Value;
            if (string.IsNullOrEmpty(videoIdStr))
            {
                Log.Error("Mini App", "解析ID为空");
                return;
            }

            //获取视频信息
            var videoInfo = VideoApis.GetVideoInfo(videoIdStr);
            if (videoInfo.Code != 0)
            {
                await eventArgs.Reply($"API发生错误({videoInfo.Code})\r\nmessage:{videoInfo.Message}");
                return;
            }

            //发送视频信息
            await eventArgs.Reply("别他妈发小程序了");
            await eventArgs.Reply(GenReplyMessage(videoInfo));
        }

        private static MessageBody GenReplyMessage(VideoInfo info)
        {
            StringBuilder messageBuilder = new();
            messageBuilder.Append($"Link:https://b23.tv/{info.Bid}\r\n");
            messageBuilder.Append($"标题:{info.Title}\r\n");
            messageBuilder.Append($"简介:{info.Desc}\r\n");
            messageBuilder.Append($"UP:{info.AuthName}\r\nhttps://space.bilbili.com/{info.AuthUid}\r\n");
            messageBuilder.Append($"投稿时间:{info.PublishTime:yyyy-MM-dd HH:mm:ss}");

            MessageBody sendMessage = $"Bilibili视频解析\r\n[{info.Bid}(av{info.Aid})]\r\n" +
                                      CQCodes.CQImage(info.CoverUrl)                    +
                                      messageBuilder.ToString();

            return sendMessage;
        }
    }
}