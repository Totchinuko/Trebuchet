﻿using Goog;
using GoogLib;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GoogGUI
{
    public class MenuElementFactory : DefaultJsonTypeInfoResolver
    {
        private readonly Config _config;
        private readonly SteamSession _steamHandler;
        private readonly Trebuchet _trebuchet;
        private readonly UIConfig _uiConfig;

        public MenuElementFactory(Config config, UIConfig uiConfig, SteamSession steamHandler, Trebuchet trebuchet)
        {
            _config = config;
            _uiConfig = uiConfig;
            _steamHandler = steamHandler;
            _trebuchet = trebuchet;
        }

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null && jsonTypeInfo.Type.IsAssignableTo(typeof(Panel)))
                jsonTypeInfo.CreateObject = () => Build(jsonTypeInfo.Type);

            return jsonTypeInfo;
        }

        private Panel Build(Type type)
        {
            switch (type.Name)
            {
                case nameof(Dashboard):
                    return new Dashboard(_config, _uiConfig, _steamHandler, _trebuchet);

                case nameof(ModlistHandler):
                    return new ModlistHandler(_config, _uiConfig, _steamHandler);

                default:
                    return (Panel)(Activator.CreateInstance(type, _config, _uiConfig) ?? throw new Exception($"Could not create a panel of type {type}"));
            }
        }
    }
}