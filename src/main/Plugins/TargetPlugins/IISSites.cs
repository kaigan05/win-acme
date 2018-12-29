﻿using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSites : ITargetPlugin
    {
        protected ILogService _log;
        protected IISClient _iisClient;
        protected IISSiteHelper _helper;
        protected IISSitesOptions _options;

        public IISSites(ILogService log, IISClient iisClient, IISSiteHelper helper, IISSitesOptions options)
        {
            _log = log;
            _iisClient = iisClient;
            _helper = helper;
            _options = options;
        }

        public Target Generate()
        {
            var sites = _helper.GetSites(false, false);
            var filtered = new List<IISSiteHelper.IISSiteOption>();
            if (_options.All == true)
            {
                filtered = sites;
            } 
            else
            {
                foreach (var id in _options.SiteIds)
                {
                    var site = sites.FirstOrDefault(s => s.Id == id);
                    if (site != null)
                    {
                        filtered.Add(site);
                    }
                    else
                    {
                        _log.Warning("Site {ID} not found", id);
                    }
                }
            }
            var allHosts = filtered.SelectMany(x => x.Hosts);
            var exclude = _options.ExcludeBindings ?? new List<string>();
            allHosts = allHosts.Except(exclude).ToList();
            var validCommonName = !string.IsNullOrEmpty(_options.CommonName) && allHosts.Contains(_options.CommonName);
            if (!validCommonName)
            {
                _log.Warning($"Specified common name {_options.CommonName} not valid");
            }
            return new Target()
            {
                CommonName = validCommonName ? _options.CommonName : allHosts.FirstOrDefault(),
                Parts = filtered.Select(site => new TargetPart {
                    Hosts = site.Hosts.Except(exclude),
                    SiteId = site.Id
                })
            };
        }
    }
}