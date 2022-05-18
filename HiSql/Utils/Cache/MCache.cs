﻿using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HiSql
{

    /// <summary>
    /// 本地Memory缓存
    /// </summary>
    public class MCache : ICache
    {
        public int DefaultExpirySecond { set; get; } = 30;
        private readonly object lockList = new object();
        private readonly Hashtable globalHSet = new Hashtable();
        private readonly object lockHstObj = new object();

        private string CacheRegion = "HI";
        internal MemoryCacheOptions MemoryCacheOptions { get; }

        public int Count => _cache.Count;

        private string _lockhashname = $"{HiSql.Constants.NameSpace}:locktable";
        private string _lockhishashname = $"{HiSql.Constants.NameSpace}:locktable_his";
        private string _lockkeyPrefix = $"{HiSql.Constants.NameSpace}:lck:";

        private string _hsetkeyPrefix = $"{HiSql.Constants.NameSpace}:";

        public MCache(MemoryCacheOptions memoryCacheOptions = null)//
        {
            MemoryCacheOptions = memoryCacheOptions ?? new MemoryCacheOptions();
            this._cache = new MemoryCache(MemoryCacheOptions);
        }
        private MemoryCache _cache;
        private string GetRegionKey(string key, string region = null)
        {
            if (region == null)
            {
                region = CacheRegion;
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.StartsWith(region + ":"))
                return key;

            if (!key.StartsWith(HiSql.Constants.NameSpace))
                key = HiSql.Constants.NameSpace + ":" + key;

            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            return fullKey;
        }

        public bool Exists(string key)
        {
            key = GetRegionKey(key);

            object v = null;
            return this._cache.TryGetValue<object>(key, out v);
        }


        public string GetCache(string key)
        {
            key = GetRegionKey(key);
            string value = string.Empty;
            this._cache.TryGetValue(key, out value);
            return value;
        }

        public T GetCache<T>(string key) where T : class
        {
            key = GetRegionKey(key);

            T v = null;
            this._cache.TryGetValue<T>(key, out v);


            return v;
        }


        public void SetCache(string key, object value)
        {
            DateTimeOffset expirationTime = DateTimeOffset.Now;
            expirationTime = expirationTime.AddSeconds(DefaultExpirySecond);
            SetCache(key, value, expirationTime);
        }

        public void SetCache(string key, object value, DateTimeOffset expirationTime)
        {
            key = GetRegionKey(key);

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            object v = null;
            if (this._cache.TryGetValue(key, out v))
                this._cache.Remove(key);

            this._cache.Set<object>(key, value, expirationTime);
        }
        public void SetCache(string key, object value, int second)
        {
            DateTimeOffset expirationTime = DateTimeOffset.Now;
            expirationTime = expirationTime.AddSeconds(second);
            SetCache(key, value, expirationTime);
        }


        public void RemoveCache(string key)
        {
            key = GetRegionKey(key);
            this._cache.Remove(key);
        }

        public void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
                GC.SuppressFinalize(this);
            }

        }
        public void Clear()
        {
            var old = this._cache;
            _cache = new MemoryCache(MemoryCacheOptions);
            old.Dispose();

        }
        public T GetOrCreate<T>(string key, Func<T> value) where T : class
        {
            return GetOrCreate(key, value, DefaultExpirySecond);
        }

        /// <summary>
        /// 获取或创建缓存 并设置有效期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public T GetOrCreate<T>(string key, Func<T> value, int second) where T : class
        {
            DateTimeOffset expirationTime = DateTimeOffset.Now;
            expirationTime = expirationTime.AddSeconds(second);
            return GetOrCreate(key, value, DefaultExpirySecond);
        }

        public T GetOrCreate<T>(string key, Func<T> value, DateTimeOffset time) where T : class
        {
            key = GetRegionKey(key);
            return this._cache.GetOrCreate<T>(key, (entry) =>
            {
                var objs = value.Invoke();
                entry.SetValue(objs);
                entry.AbsoluteExpiration = time;
                return objs;

            });
        }

        public bool HSet(string hashkey, string key, string value)
        {
            if (!hashkey.Contains(_hsetkeyPrefix))
            {
                hashkey = _hsetkeyPrefix + hashkey;
            }
            hashkey = GetRegionKey(hashkey);

            if (!globalHSet.ContainsKey(hashkey))
            {
                lock (lockHstObj)
                {
                    if (!globalHSet.ContainsKey(hashkey))
                    {
                        globalHSet.Add(hashkey, new Hashtable { { key, value } });
                        return true;
                    }
                }
            }
            Hashtable keyValuePairs = (Hashtable)globalHSet[hashkey];
            if (keyValuePairs.ContainsKey(key))
            {
                keyValuePairs[key] = value;
            }
            else
            {
                keyValuePairs.Add(key, value);
            }
            globalHSet[hashkey] = keyValuePairs;
            return true;
        }

        public string HGet(string hashkey, string key)
        {
            if (!hashkey.Contains(_hsetkeyPrefix))
            {
                hashkey = _hsetkeyPrefix + hashkey;
            }
            hashkey = GetRegionKey(hashkey);
            if (globalHSet.ContainsKey(hashkey))
            {
                Hashtable keyValuePairs = (Hashtable)globalHSet[hashkey];
                return keyValuePairs.ContainsKey(key) ? keyValuePairs[key]?.ToString() : null;
            }
            return null;
        }

        public bool HDel(string hashkey, string key)
        {
            if (!hashkey.Contains(_hsetkeyPrefix))
            {
                hashkey = _hsetkeyPrefix + hashkey;
            }
            hashkey = GetRegionKey(hashkey);

            if (globalHSet.ContainsKey(hashkey))
            {
                Hashtable keyValuePairs = (Hashtable)globalHSet[hashkey];
                if (keyValuePairs.ContainsKey(key))
                    keyValuePairs.Remove(key);
            }
            return false;
        }


        public Tuple<bool, string> LockOn(string key, LckInfo lckinfo, int expresseconds = 30, int timeoutseconds = 5)
        {
            if (!key.Contains(_lockkeyPrefix))
                key = _lockkeyPrefix + key;

            key = GetRegionKey(key);

            int _max_second = 60;//最长定锁有效期
            int _max_timeout = 10;//最长加锁等待时间

            expresseconds = expresseconds < 0 ? 5 : expresseconds;
            expresseconds = expresseconds > _max_second ? _max_second : expresseconds;

            timeoutseconds = timeoutseconds < 0 ? 5 : timeoutseconds;
            timeoutseconds = timeoutseconds > _max_timeout ? _max_timeout : timeoutseconds;

            string msg = "";
            var flag1 = false;
            //创建key
            if (GetCache<LckInfo>(key) == null)
            {
                lock (lockList)
                {
                    if (GetCache<LckInfo>(key) == null)
                    {
                        SetCache(key, lckinfo, expresseconds);
                    }
                }
            }

            var isok = System.Threading.Monitor.TryEnter(GetCache<LckInfo>(key), TimeSpan.FromSeconds(timeoutseconds));
            if (!isok)
            {
                flag1 = false;
                msg = $"key:[{key}]锁定失败,加锁等待超过{timeoutseconds}秒!";
            }
            else
            {
                HSet(_lockhashname, key, JsonConvert.SerializeObject(lckinfo));
                HSet(_lockhishashname, $"{key}:{lckinfo.LockTime.ToString("yyyyMMddHHmmssfff")}", JsonConvert.SerializeObject(lckinfo));
                msg = "加锁成功";
                flag1 = true;
            }
            return new Tuple<bool, string>(flag1, msg);
        }

        public Tuple<bool, string> LockOn(string[] keys, LckInfo lckinfo, int expresseconds = 30, int timeoutseconds = 5)
        {
            Tuple<bool, string> tuple = new Tuple<bool, string>(false, "");
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> lockedKey = new List<string>();
            int idx = 0;
            while (!tuple.Item1 && stopwatch.Elapsed <= TimeSpan.FromSeconds(5) && idx < keys.Length)
            {
                var lockResult = LockOn(keys[idx], lckinfo, expresseconds, timeoutseconds);
                if (!lockResult.Item1)
                {
                    tuple = lockResult;
                    break;
                }

                lockedKey.Add(keys[idx]);
                idx++;
            }
            if (idx == keys.Length)
            {
                return new Tuple<bool, string>(true, $"key:[{string.Join(",", keys)}]加锁成功");
            }
            //加锁失败或超时
            foreach (var key in lockedKey)
            {
                UnLock(key);
            }
            return new Tuple<bool, string>(tuple.Item1, tuple.Item2);

        }

        private Tuple<bool, string> CheckLock(string key)
        {
            string _key = key;
            if (!key.Contains(_lockkeyPrefix))
                key = _lockkeyPrefix + key;

            key = GetRegionKey(key);

            bool _islock = false;
            string _msg = "";
            if (GetCache<LckInfo>(key) != null)
            {
                _islock = true;
                string _lockinfo = HGet(_lockhashname, key);
                if (!string.IsNullOrEmpty(_lockinfo))
                {
                    LckInfo lckInfo = JsonConvert.DeserializeObject<LckInfo>(_lockinfo);
                    _msg = $"key:[{_key}]已经被[{lckInfo.UName}]在[{lckInfo.EventName}]于[{lckInfo.LockTime.ToString("yyyy-MM-dd HH:mm:ss")}]锁定!";

                }
                else
                    _msg = $"key:[{_key}]被锁定";
            }
            else
            {
                _msg = $"key:[{_key}]未被锁定";
            }
            return new Tuple<bool, string>(_islock, _msg);
        }

        public Tuple<bool, string> CheckLock(params string[] keys)
        {
            foreach (var key in keys)
            {
                var res = CheckLock(key);
                if (res.Item1)
                {
                    return res;
                }
            }
            return new Tuple<bool, string>(false, $"key:[{string.Join(",", keys)}]未被锁定");
        }
        public Tuple<bool, string> LockOnExecute(string key, Action action, LckInfo lckinfo, int expresseconds = 30, int timeoutseconds = 5)
        {
            if (!key.Contains(_lockkeyPrefix))
                key = _lockkeyPrefix + key;

            key = GetRegionKey(key);

            int _max_second = 60;//最长定锁有效期
            int _max_timeout = 10;//最长加锁等待时间

            int _times = 5;//续锁最多次数

            int _millsecond = 1000;

            expresseconds = expresseconds < 0 ? 5 : expresseconds;
            expresseconds = expresseconds > _max_second ? _max_second : expresseconds;

            timeoutseconds = timeoutseconds < 0 ? 5 : timeoutseconds;
            timeoutseconds = timeoutseconds > _max_timeout ? _max_timeout : timeoutseconds;

            string msg = "";
            var flag1 = false;
            //创建key
            if (GetCache<LckInfo>(key) == null)
            {
                lock (lockList)
                {
                    if (GetCache<LckInfo>(key) == null)
                    {
                        SetCache(key, lckinfo, expresseconds);
                    }
                }
            }
            bool flag = false;

            var isgetlock = System.Threading.Monitor.TryEnter(GetCache<LckInfo>(key), TimeSpan.FromSeconds(timeoutseconds));
            if (!isgetlock)
            {
                flag1 = false;
                msg = $"key:[{key}]锁定失败,加锁等待超过{timeoutseconds}秒!";
            }
            else
            {
                HSet(_lockhashname, key, JsonConvert.SerializeObject(lckinfo));
                HSet(_lockhishashname, $"{key}:{lckinfo.LockTime.ToString("yyyyMMddHHmmssfff")}", JsonConvert.SerializeObject(lckinfo));

                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = tokenSource.Token;

                Thread thread = null;
                var workTask = Task.Run(() =>
                {
                    thread = System.Threading.Thread.CurrentThread;
                    try
                    {
                        if (action != null)
                        {
                            action.Invoke();
                        }

                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"线程中断。。");
                    }
                    finally
                    {
                       
                    }
                });

                flag = workTask.Wait(timeoutseconds * _millsecond, cancellationToken);
                if (flag)
                {
                    UnLock(key);
                    msg = $"key:[{key}]锁定并操作业务成功!锁已自动释放";
                }
                else
                {
                    var _timesa = 0;
                    while (!flag)
                    {
                        if (_timesa >= _times)
                        {
                            if (!workTask.IsCompleted && !workTask.IsCanceled)
                            {
                                UnLock(key);
                                tokenSource.Cancel();
                                thread.Interrupt();
                            }
                            flag = false;
                            msg = $"key:[{key}]锁定操作业务失败!超过最大[{_times}]次续锁没有完成,操作被撤销";
                            break;
                        }
                        else
                        {
                            //续锁
                            SetCache(key, lckinfo, expresseconds);
                            lckinfo = getLockInfo(lckinfo, expresseconds, _timesa);
                            HSet(_lockhashname, key, JsonConvert.SerializeObject(lckinfo));
                            HSet(_lockhishashname, $"{key}:{lckinfo.LockTime.ToString("yyyyMMddHHmmssfff")}", JsonConvert.SerializeObject(lckinfo));
                            flag = workTask.Wait(timeoutseconds * _millsecond, cancellationToken);
                            if (flag)
                            {
                                flag = true;
                                UnLock(key);
                                msg = $"key:[{key}]锁定并操作业务成功!续锁{_timesa + 1}次,锁已经自动释放";
                                break;
                            }
                        }
                        _timesa++;
                    }

                    if (!flag)
                    {
                        msg = $"key:[{key}]锁定操作业务失败!超过最大[{_times}]次续锁没有完成,操作被撤销";
                    }
                }
            }

            return new Tuple<bool, string>(flag, msg);
        }


        public Tuple<bool, string> LockOnExecute(string[] keys, Action action, LckInfo lckinfo, int expresseconds = 30, int timeoutseconds = 5)
        {

            int _max_second = 60;//最长定锁有效期
            int _max_timeout = 10;//最长加锁等待时间

            int _times = 5;//续锁最多次数

            int _millsecond = 1000;

            expresseconds = expresseconds < 0 ? 5 : expresseconds;
            expresseconds = expresseconds > _max_second ? _max_second : expresseconds;

            timeoutseconds = timeoutseconds < 0 ? 5 : timeoutseconds;
            timeoutseconds = timeoutseconds > _max_timeout ? _max_timeout : timeoutseconds;

            string msg = "";
            var flag1 = false;

            bool flag = false;

            var getlockResult = LockOn(keys, lckinfo, expresseconds, timeoutseconds);
            if (!getlockResult.Item1)
            {
                flag1 = false;
                msg = getlockResult.Item2;
            }
            else
            {

                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = tokenSource.Token;
                Thread thread = null;
                var workTask = Task.Run(() =>
                {
                    thread = System.Threading.Thread.CurrentThread;
                    try
                    {
                        if (action != null)
                        {
                            action.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"线程中断。。");
                    }
                    finally
                    {
                        //UnLock(keys); 不可以再此处解锁，否则会提示跨现场错误
                    }
                });

                flag = workTask.Wait(timeoutseconds * _millsecond, cancellationToken);
                if (flag)
                {
                    UnLock(keys);
                    msg = $"key:[{string.Join(",", keys)}]锁定并操作业务成功!锁已自动释放";
                }
                else
                {
                    var _timesa = 0;
                    while (!flag)
                    {
                        if (_timesa >= _times)
                        {
                            if (!workTask.IsCompleted && !workTask.IsCanceled)
                            {
                                UnLock(keys);
                                tokenSource.Cancel();
                                thread.Interrupt();
                            }
                            flag = false;
                            msg = $"key:[{string.Join(",", keys)}]锁定操作业务失败!超过最大[{_times}]次续锁没有完成,操作被撤销";
                            break;
                        }
                        else
                        {
                            //续锁
                            foreach (var key in keys)
                            {
                                var newKey = key;
                                if (!newKey.Contains(_lockkeyPrefix))
                                    newKey = _lockkeyPrefix + newKey;

                                newKey = GetRegionKey(newKey);

                                SetCache(newKey, lckinfo, expresseconds);
                                lckinfo = getLockInfo(lckinfo, expresseconds, _timesa);
                                HSet(_lockhashname, newKey, JsonConvert.SerializeObject(lckinfo));
                                HSet(_lockhishashname, $"{newKey}:{lckinfo.LockTime.ToString("yyyyMMddHHmmssfff")}", JsonConvert.SerializeObject(lckinfo));
                            }
                            flag = workTask.Wait(timeoutseconds * _millsecond, cancellationToken);
                            if (flag)
                            {
                                UnLock(keys);
                                flag = true;
                                msg = $"key:[{string.Join(",", keys)}]锁定并操作业务成功!续锁{_timesa + 1}次,锁已经自动释放";
                                break;
                            }
                        }
                        _timesa++;
                    }

                    if (!flag)
                    {
                        msg = $"key:[{keys}]锁定操作业务失败!超过最大[{_times}]次续锁没有完成,操作被撤销";
                    }
                }
            }

            return new Tuple<bool, string>(flag, msg);
        }
        LckInfo getLockInfo(LckInfo lckInfo, int expresseconds, int times)
        {
            if (lckInfo.LockTime == null || lckInfo.LockTime == DateTime.MinValue)
                lckInfo.LockTime = DateTime.Now;

            if (lckInfo.ExpireTime == null || lckInfo.ExpireTime == DateTime.MinValue)
            {
                lckInfo.ExpireTime = DateTime.Now.AddSeconds(expresseconds);
            }
            else
                lckInfo.ExpireTime = lckInfo.ExpireTime.AddSeconds(expresseconds);

            lckInfo.KeepTimes = times;


            return lckInfo;
        }
        public bool UnLock(params string[] keys)
        {
            foreach (string key in keys)
            {
                var newkey = key;
                if (!newkey.Contains(_lockkeyPrefix))
                    newkey = _lockkeyPrefix + newkey;
                newkey = GetRegionKey(newkey);
                //创建key
                if (GetCache<LckInfo>(newkey) != null)
                {
                    System.Threading.Monitor.Exit(GetCache<LckInfo>(newkey));
                    RemoveCache(newkey);
                    HDel(_lockhashname, newkey);
                }
            }

            return true;
        }

        public List<LckInfo> GetCurrLockInfo()
        {
            List<LckInfo> lckInfos = new List<LckInfo>();
            var newkey = GetRegionKey(_lockhashname);
            if (globalHSet.ContainsKey(newkey))
            {
                List<string> lckInfosWaitRemove = new List<string>();
                var list = (Hashtable)globalHSet[newkey];
                foreach (string key in list.Keys)
                {
                    if (GetCache<LckInfo>(key) != null && list[key] != null)
                    {
                        LckInfo lckInfo = JsonConvert.DeserializeObject<LckInfo>(list[key]?.ToString());
                        lckInfo.Key = key;
                        lckInfos.Add(lckInfo);
                    }
                    else
                    {
                        lckInfosWaitRemove.Add(key);

                    }
                }

                foreach (var key in lckInfosWaitRemove)
                {
                    HDel(_lockhashname, key);
                }
            }
            return lckInfos;
        }

        public List<LckInfo> GetHisLockInfo()
        {
            var newkey = GetRegionKey(_lockhishashname);

            List<LckInfo> lckInfos = new List<LckInfo>();
            if (globalHSet.ContainsKey(newkey))
            {
                var list = (Hashtable)globalHSet[newkey];
                foreach (string key in list.Keys)
                {
                    if (list[key] != null)
                    {
                        LckInfo lckInfo = JsonConvert.DeserializeObject<LckInfo>(list[key]?.ToString());
                        lckInfo.Key = key;
                        lckInfos.Add(lckInfo);
                    }

                }
            }
            return lckInfos;
        }
    }
}
