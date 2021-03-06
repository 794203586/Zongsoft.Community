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
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Community.Models
{
	public class PostConditional : Zongsoft.Data.Conditional
	{
		#region 公共属性
		[Conditional("Content")]
		public string Key
		{
			get
			{
				return this.GetPropertyValue(() => this.Key);
			}
			set
			{
				this.SetPropertyValue(() => this.Key, value);
			}
		}

		public string Content
		{
			get
			{
				return this.GetPropertyValue(() => this.Content);
			}
			set
			{
				this.SetPropertyValue(() => this.Content, value);
			}
		}

		public ulong? ParentId
		{
			get
			{
				return this.GetPropertyValue(() => this.ParentId);
			}
			set
			{
				this.SetPropertyValue(() => this.ParentId, value);
			}
		}

		public bool? Disabled
		{
			get
			{
				return this.GetPropertyValue(() => this.Disabled);
			}
			set
			{
				this.SetPropertyValue(() => this.Disabled, value);
			}
		}

		public bool? IsApproved
		{
			get
			{
				return this.GetPropertyValue(() => this.IsApproved);
			}
			set
			{
				this.SetPropertyValue(() => this.IsApproved, value);
			}
		}

		public bool? IsLocked
		{
			get
			{
				return this.GetPropertyValue(() => this.IsLocked);
			}
			set
			{
				this.SetPropertyValue(() => this.IsLocked, value);
			}
		}

		public bool? IsValued
		{
			get
			{
				return this.GetPropertyValue(() => this.IsValued);
			}
			set
			{
				this.SetPropertyValue(() => this.IsValued, value);
			}
		}

		public uint? CreatorId
		{
			get
			{
				return this.GetPropertyValue(() => this.CreatorId);
			}
			set
			{
				this.SetPropertyValue(() => this.CreatorId, value);
			}
		}

		public ConditionalRange<DateTime> CreatedTime
		{
			get
			{
				return this.GetPropertyValue(() => this.CreatedTime);
			}
			set
			{
				this.SetPropertyValue(() => this.CreatedTime, value);
			}
		}
		#endregion
	}
}
