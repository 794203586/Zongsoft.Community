﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <9555843@qq.com>
 * 
 * Copyright (C) 2015-2017 Zongsoft Corporation. All rights reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Community.Models;

namespace Zongsoft.Community.Services
{
	[DataSequence("ThreadId", 100000)]
	[DataSearchKey("Key:Subject")]
	public class ThreadService : ServiceBase<Thread>
	{
		#region 成员字段
		private PostService _posting;
		#endregion

		#region 构造函数
		public ThreadService(Zongsoft.Services.IServiceProvider serviceProvider) : base(serviceProvider)
		{
		}
		#endregion

		#region 公共属性
		public PostService Posting
		{
			get
			{
				if(_posting == null)
					_posting = this.ServiceProvider.ResolveRequired<PostService>();

				return _posting;
			}
		}
		#endregion

		#region 公共方法
		public IEnumerable<Post> GetPosts(ulong threadId, Paging paging = null)
		{
			var thread = this.DataAccess.Select<Thread>(Condition.Equal("ThreadId", threadId)).FirstOrDefault();

			if(thread == null)
				return Enumerable.Empty<Post>();

			var conditions = ConditionCollection.And(
				Condition.Equal("ThreadId", threadId),
				Condition.NotEqual("PostId", thread.PostId));

			var posts = this.DataAccess.Select<Post>(conditions, paging, Sorting.Descending("PostId"));

			foreach(var post in posts)
			{
				if(post == null)
					continue;

				if(post.IsApproved)
				{
					//如果内容类型是外部文件（即非嵌入格式），则读取文件内容
					if(!Utility.IsContentEmbedded(post.ContentType))
						post.Content = Utility.ReadTextFile(post.Content);
				}
				else
				{
					post.Content = null;
				}

				//设置帖子的附件集
				post.Attachments = this.DataAccess.Select<Post.PostAttachment>(Condition.Equal("PostId", post.PostId), "File");
			}

			return posts;
		}
		#endregion

		#region 重写方法
		protected override Thread OnGet(ICondition condition, string scope, object state)
		{
			if(string.IsNullOrWhiteSpace(scope))
				scope = "Post, Creator, Creator.User";

			//调用基类同名方法
			var thread = base.OnGet(condition, scope, state);

			if(thread == null)
				return null;

			//如果当前主题是禁用或者未审核的，则需要进行权限判断
			if(thread.Disabled || (!thread.IsApproved))
			{
				//判断当前用户是否为该论坛的版主
				var isModerator = this.ServiceProvider.ResolveRequired<ForumService>().IsModerator(thread.SiteId, thread.ForumId);

				if(!isModerator)
				{
					//当前用户不是版主，并且该主题已被禁用则返回空（相当于主题不存在）
					if(thread.Disabled)
						return null;

					//当前用户不是版主，并且该主题未审核则抛出授权异常
					if(!thread.IsApproved)
						throw new Zongsoft.Security.Membership.AuthorizationException();
				}
			}

			//递增当前主题的累计阅读量
			this.Increment("TotalViews", Condition.Equal("ThreadId", thread.ThreadId));

			//更新当前用户的浏览记录
			this.SetHistory(thread.ThreadId);

			//更新主题帖子的相关信息
			if(thread.Post != null)
			{
				//设置主题对应的帖子内容（如果内容类型是外部文件(即非嵌入格式)，则读取文件内容）
				if(!Utility.IsContentEmbedded(thread.Post.ContentType))
					thread.Post.Content = Utility.ReadTextFile(thread.Post.Content);

				//获取主题对应帖子的投票记录
				var votes = this.DataAccess.Select<Post.PostVoting>(Condition.Equal("PostId", thread.PostId)).ToArray();

				//设置主题对应帖子的点赞集
				thread.Post.Upvotes = votes.Where(p => p.Value > 0);
				//设置主题对应帖子的被踩集
				thread.Post.Downvotes = votes.Where(p => p.Value < 0);

				//设置主题对应帖子的附件集
				thread.Post.Attachments = this.DataAccess.Select<Post.PostAttachment>(Condition.Equal("PostId", thread.PostId), "File");
			}

			return thread;
		}

		protected override IEnumerable<Thread> OnSelect(ICondition condition, string scope, Paging paging, Sorting[] sortings, object state)
		{
			if(string.IsNullOrWhiteSpace(scope))
				scope = "Creator, Creator.User";

			//调用基类同名方法
			return base.OnSelect(condition, scope, paging, sortings, state);
		}

		protected override int OnDelete(ICondition condition, string[] cascades, object state)
		{
			var ids = this.DataAccess.Select<Thread>(this.Name, condition, Paging.Disable).Select(p => p.ThreadId).ToArray();

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				//调用基类同名方法
				var count = base.OnDelete(condition, cascades, state);

				if(count > 0)
					this.DataAccess.Delete<Post>(Condition.In("ThreadId", ids));

				//提交事务
				transaction.Commit();

				//返回被删除的主题记录数
				return count;
			}
		}

		protected override int OnInsert(DataDictionary<Thread> data, string scope, object state)
		{
			var post = data.Get(p => p.Post, null);

			if(post == null || string.IsNullOrEmpty(post.Content))
				throw new InvalidOperationException("Missing content of the thread.");

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				//设置主题内容贴编号为零
				data.Set(p => p.PostId, (ulong)0);

				//调用基类同名方法，插入主题数据
				var count = base.OnInsert(data, scope, state);

				if(count < 1)
					return count;

				//更新主题内容贴的相关属性
				post.ThreadId = data.Get(p => p.ThreadId);
				post.SiteId = data.Get(p => p.SiteId);
				post.CreatorId = data.Get(p => p.CreatorId);
				post.CreatedTime = data.Get(p => p.CreatedTime);

				//通过帖子服务来新增主题的内容贴
				count = this.Posting.Insert(post, data);

				//如果主题内容贴新增成功则提交事务
				if(count > 0)
				{
					//更新新增主题的内容帖子编号
					this.DataAccess.Update(this.Name, new
					{
						ThreadId = data.Get(p => p.ThreadId),
						PostId = post.PostId,
					});

					//更新主题数据字典中的内容帖子编号
					data.Set(p => p.PostId, post.PostId);

					//更新发帖人关联的主题统计信息
					this.SetMostRecentThread(data);

					//提交事务
					transaction.Commit();
				}

				return count;
			}
		}

		protected override int OnUpdate(DataDictionary<Thread> data, ICondition condition, string scope, object state)
		{
			//调用基类同名方法
			var count = base.OnUpdate(data, condition, scope, state);

			//获取要更新的主题内容贴
			var post = data.Get(p => p.Post, null);

			if(post != null)
			{
				if(post.PostId == 0)
				{
					//优先从主题实体中获取对应的内容贴编号
					if(data.TryGet(p => p.PostId, out var postId) && postId != 0)
					{
						post.PostId = postId;
					}
					else
					{
						//获取修改主题对应的主题对象
						var thread = this.DataAccess.Select<Thread>(Condition.Equal("ThreadId", data.Get(p => p.ThreadId)), "!, ThreadId, PostId").FirstOrDefault();

						if(thread == null)
							return count;

						//更新主题内容贴的编号
						post.PostId = thread.PostId;
					}
				}

				return this.Posting.Update(post);
			}

			return count;
		}
		#endregion

		#region 私有方法
		private bool SetMostRecentThread(DataDictionary<Thread> data)
		{
			if(data == null)
				return false;

			var userId = data.Get(p => p.CreatorId);
			var user = Utility.GetUser(userId, this.EnsureCredential());
			var count = 0;

			//更新当前主题所属论坛的最后发帖信息
			count += this.DataAccess.Update(this.DataAccess.Naming.Get<Forum>(), new
			{
				SiteId = data.Get(p => p.SiteId),
				ForumId = data.Get(p => p.ForumId),
				MostRecentThreadId = data.Get(p => p.ThreadId),
				MostRecentThreadSubject = data.Get(p => p.Subject),
				MostRecentThreadTime = data.Get(p => p.CreatedTime),
				MostRecentThreadAuthorId = userId,
				MostRecentThreadAuthorName = user?.FullName,
				MostRecentThreadAuthorAvatar = user?.Avatar,
			});

			//递增当前发帖人的累计主题数，并且更新发帖人的最后发表的主题信息
			if(this.DataAccess.Increment<UserProfile>("TotalThreads", Condition.Equal("UserId", data.Get(p => p.CreatorId))) > 0)
			{
				count += this.DataAccess.Update(this.DataAccess.Naming.Get<UserProfile>(), new
				{
					UserId = data.Get(p => p.CreatorId),
					MostRecentThreadId = data.Get(p => p.ThreadId),
					MostRecentThreadSubject = data.Get(p => p.Subject),
					MostRecentThreadTime = data.Get(p => p.CreatedTime),
				});
			}

			return count > 0;
		}

		private void SetHistory(ulong threadId)
		{
			var credential = this.EnsureCredential();
			var conditions = Condition.Equal("UserId", credential.UserId) & Condition.Equal("ThreadId", threadId);

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				//递增当前用户对当前主题的累计浏览量
				if(this.DataAccess.Increment<History>("Count", conditions) > 0)
				{
					//更新当前用户对当前主题的最后浏览时间
					this.DataAccess.Update(this.DataAccess.Naming.Get<History>(), new
					{
						MostRecentViewedTime = DateTime.Now,
					}, conditions);
				}
				else
				{
					//尝试新增一条用户的浏览记录
					this.DataAccess.Insert(new History(credential.UserId, threadId));
				}
			}
		}
		#endregion
	}
}
