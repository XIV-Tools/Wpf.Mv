﻿// © XIV-Tools.
// Licensed under the MIT license.

namespace XivToolsWpf.Services
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json;
	using Serilog;

	public delegate void LocalizationEvent();

	public class LocalizationService
	{
		public const string FallbackCulture = "EN";

		private static readonly Dictionary<string, Locale> Locales = new Dictionary<string, Locale>();

		private static Locale? fallbackLocale;
		private static Locale? currentLocale;

		public LocalizationService()
		{
			Add("Languages");

			currentLocale = Locales[FallbackCulture];
			fallbackLocale = currentLocale;

			Locales.Add("GIB", new GiberishLocale(fallbackLocale));
		}

		public static event LocalizationEvent? LocaleChanged;

		public static void Add(string culture, string key, string value)
		{
			culture = culture.ToUpperInvariant();

			if (!Locales.ContainsKey(culture))
				Locales.Add(culture, new Locale(culture));

			Locale locale = Locales[culture];

			Locales[culture].Add(key, value);
		}

		public static void Add(string culture, Dictionary<string, string> values)
		{
			foreach ((string key, string value) in values)
			{
				Add(culture, key, value);
			}
		}

		public static void Add(string searchPath)
		{
			string[] paths = Directory.GetFiles(searchPath);

			foreach (string path in paths)
			{
				string culture = Path.GetFileNameWithoutExtension(path);

				string json = File.ReadAllText(path);
				Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
				Add(culture, values);
			}
		}

		public static bool HasString(string key)
		{
			string val;
			if (currentLocale?.Get(key, out val) != true)
				return false;

			return true;
		}

		public static string GetStringFormatted(string key, params string[] param)
		{
			string str = GetString(key);
			return string.Format(str, param);
		}

		public static string GetString(string key, bool silent = false)
		{
			string val = string.Empty;

			if (currentLocale?.Get(key, out val) ?? false)
				return val;

			if (!silent)
				Log.Warning("Missing Localized string: \"" + key + "\" in locale: \"" + currentLocale?.Culture + "\"");

			if (fallbackLocale?.Get(key, out val) ?? false)
				return val;

			if (!silent)
				Log.Error("Missing Localized string: \"" + key + "\"");

			if (!silent)
			{
				string erorString = "{" + key + "}";
				fallbackLocale?.Add(key, erorString);
				return erorString;
			}
			else
			{
				fallbackLocale?.Add(key, string.Empty);
				return string.Empty;
			}
		}

		public static void SetLocale(string locale)
		{
			locale = locale.ToUpper();

			if (Locales.ContainsKey(locale))
			{
				currentLocale = Locales[locale];
			}
			else
			{
				currentLocale = fallbackLocale;
			}

			LocaleChanged?.Invoke();
		}

		public static Dictionary<string, string> GetAvailableLocales()
		{
			Dictionary<string, string> results = new Dictionary<string, string>();
			foreach (Locale locale in Locales.Values)
			{
				if (results.ContainsKey(locale.Culture))
					continue;

				results.Add(locale.Culture, locale.Name);
			}

			return results;
		}

		private class Locale
		{
			public readonly string Culture;
			public string Name;

			private readonly Dictionary<string, string> values = new Dictionary<string, string>();

			public Locale(string culture)
			{
				this.Culture = culture;
				this.Name = culture;
			}

			public virtual void Add(string key, string value)
			{
				if (!this.values.ContainsKey(key))
					this.values.Add(key, value);

				this.values[key] = value;
			}

			public virtual bool Get(string key, out string value)
			{
				value = string.Empty;

				if (!this.values.ContainsKey(key))
					return false;

				value = this.values[key];
				return true;
			}
		}

		/// <summary>
		/// Converts any locale into gibberish English.
		/// </summary>
		private class GiberishLocale : Locale
		{
			private const string Characters = @"abcdefghijklmnopqrstuvwxyz";
			private static readonly Random Random = new Random();

			private readonly Locale baseLocale;

			public GiberishLocale(Locale baseLocale)
				: base("Gib")
			{
				this.baseLocale = baseLocale;
				this.Name = "Gibberish";
			}

			public override bool Get(string key, out string value)
			{
				if (!base.Get(key, out value))
				{
					string? str = null;

					if (!this.baseLocale.Get(key, out str))
						return false;

					if (str == null)
						return false;

					char[] newStr = new char[str.Length];

					for (int i = 0; i < str.Length; i++)
					{
						if (char.IsDigit(str[i]))
						{
							newStr[i] = Random.Next(0, 9).ToString()[0];
						}
						else if (char.IsLetter(str[i]))
						{
							newStr[i] = Characters[Random.Next(0, Characters.Length)];

							if (char.IsUpper(str[i]))
							{
								newStr[i] = char.ToUpper(newStr[i]);
							}
						}
						else
						{
							newStr[i] = str[i];
						}
					}

					value = new string(newStr);
					this.Add(key, value);
				}

				return true;
			}
		}
	}
}