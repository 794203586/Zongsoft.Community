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

namespace Zongsoft.Community.Configuration
{
	/// <summary>
	/// 表示应用配置信息的接口。
	/// </summary>
	public interface IConfiguration
	{
		/// <summary>
		/// 获取或设置文件存储的基路径。
		/// </summary>
		string BasePath
		{
			get;
			set;
		}

		/// <summary>
		/// 获取指定站点的文件存储基路径。
		/// </summary>
		/// <param name="siteId">指定的站点编号。</param>
		/// <returns>返回的站点基路径。</returns>
		string GetSitePath(uint siteId);
	}
}
