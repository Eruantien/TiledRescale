using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TiledRescale
{
	public class Rescaler
	{
		public string RescaleMap(string fileName, int? requiredWidth, int? requiredHeight, float? scale)
		{
			string message;

			try
			{
				var xdoc = XDocument.Load(fileName);

				var result = ProcessXDoc(xdoc, requiredWidth, requiredHeight, scale);

				if (result)
				{
					xdoc.Save(fileName);
					message = "Rescaled";
				}
				else
				{
					message = "Invalid";
				}

			}
			catch (Exception ex)
			{
				message = ex.ToString();
			}

			return $"{fileName} - {message}";
		}

		private bool ProcessXDoc(XContainer xdoc, int? requiredWidth, int? requiredHeight, float? scale)
		{
			var descMap = xdoc.Descendants("map").FirstOrDefault();

			if (descMap == null) return false;

			var oldMapWidth = GetFloatValueFromAttribute(descMap.Attribute("width"));
			var oldMapHeight = GetFloatValueFromAttribute(descMap.Attribute("height"));

			if (oldMapWidth == null || oldMapHeight == null) throw new Exception("Map element missing Width / Height");

			float multiplierHorizontal;
			float multiplierVertical;

			if (scale.HasValue)
			{
				multiplierHorizontal = scale.Value;
				multiplierVertical = scale.Value;
			}
			else if (requiredWidth.HasValue && requiredHeight.HasValue)
			{
				multiplierHorizontal = requiredWidth.Value / (float)oldMapWidth;
				multiplierVertical = requiredHeight.Value / (float)oldMapHeight;
			}
			else
			{
				throw new Exception("Require new dimensions or scale - missing both.");
			}

			AttemptAttributeIntRescale(descMap, "width", multiplierHorizontal);
			AttemptAttributeIntRescale(descMap, "height", multiplierVertical);

			ProcessLayers(xdoc, multiplierHorizontal, multiplierVertical);
			ProcessObjectGroups(xdoc, multiplierHorizontal, multiplierVertical);

			return true;
		}

		private void ProcessObjectGroups(XContainer xdoc, float multiplierHorizontal, float multiplierVertical)
		{
			foreach (var descObjectGroup in xdoc.Descendants("objectgroup"))
			{
				foreach (var descObject in descObjectGroup.Descendants("object"))
				{
					AttemptAttributeFloatRescale(descObject, "x", multiplierHorizontal);
					AttemptAttributeFloatRescale(descObject, "y", multiplierHorizontal);
					AttemptAttributeFloatRescale(descObject, "width", multiplierHorizontal);
					AttemptAttributeFloatRescale(descObject, "height", multiplierHorizontal);

					foreach (var descPolyLine in descObject.Descendants("polyline"))
					{
						var attributePoints = descPolyLine.Attribute("points");

						if (attributePoints == null) continue;

						var pointsString = attributePoints.Value;
						var pointsArray = pointsString.Split(' ');

						for (var i = 0; i < pointsArray.Length; i++)
						{
							var vectorArray = pointsArray[i].Split(',');

							var x = float.Parse(vectorArray[0]);
							var y = float.Parse(vectorArray[1]);

							x = x * multiplierHorizontal;
							y = y * multiplierVertical;

							var xRounded = Math.Round(x, 2);
							var yRounded = Math.Round(y, 2);

							pointsArray[i] = $"{xRounded},{yRounded}";
						}

						attributePoints.Value = string.Join(' ', pointsArray);
					}
				}
			}
		}

		private void ProcessLayers(XContainer xdoc, float multiplierHorizontal, float multiplierVertical)
		{
			foreach (var descLayer in xdoc.Descendants("layer"))
			{
				AttemptAttributeIntRescale(descLayer, "width", multiplierHorizontal);
				AttemptAttributeIntRescale(descLayer, "height", multiplierVertical);
				var newWidth = GetFloatValueFromAttribute(descLayer.Attribute("width"));
				var newHeight = GetFloatValueFromAttribute(descLayer.Attribute("height"));

				if (newWidth == null || newHeight == null) throw new Exception("Layer element missing Width / Height");

				var descs = descLayer.Descendants();
				foreach (var elementData in descs.Where(x => x.Name.LocalName == "data"))
				{
					var dataLines = GetMapDataLines(elementData);

					dataLines = PerformVerticalRescale(dataLines, 1 / multiplierVertical, (int)newHeight.Value);
					dataLines = PerformHorizontalRescale(dataLines, 1 / multiplierHorizontal, (int)newWidth.Value);

					SetMapData(elementData, dataLines);
				}
			}
		}

		private List<string> GetMapDataLines(XElement element)
		{
			var str = element.Value;
			var lines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
			lines.RemoveAll(string.IsNullOrEmpty);
			return lines;
		}

		private void SetMapData(XElement element, IEnumerable<string> dataLines)
		{
			var newstring = string.Join("\n", dataLines);
			element.Value = newstring;
		}

		private List<string> PerformVerticalRescale(IReadOnlyList<string> lines, float multiplierInverseVertical, int rowCount)
		{
			var linesTransformed = new List<string>();

			for (var y = 0; y < rowCount; y++)
			{
				var trailingY = (int)(y * multiplierInverseVertical);
				var trailingLine = lines[trailingY];
				linesTransformed.Add(trailingLine);
			}

			return linesTransformed;
		}

		private List<string> PerformHorizontalRescale(List<string> lines, float multiplierInverseHorizontal, int columnCount)
		{
			for (var i = 0; i < lines.Count; i++)
			{
				var lineParts = lines[i].Split(',').ToList();
				lineParts.RemoveAll(string.IsNullOrEmpty);

				var sb = new StringBuilder();

				for (var x = 0; x < columnCount; x++)
				{
					var trailingX = (int)(x * multiplierInverseHorizontal);
					var trailingValue = lineParts[trailingX];
					sb.Append(trailingValue);

					if (x == columnCount - 1 && i == lines.Count - 1) continue;

					sb.Append(",");
				}

				lines[i] = sb.ToString();
			}

			return lines;
		}

		private void AttemptAttributeIntRescale(XElement element, string attributeName, float multiplier)
		{
			var xAttribute = element.Attribute(attributeName);
			if (xAttribute == null) return;

			var value = GetFloatValueFromAttribute(xAttribute);
			if (value == null) return;

			var scaledValue = Math.Ceiling(value.Value * multiplier);

			xAttribute.Value = scaledValue.ToString(CultureInfo.InvariantCulture);
		}

		private void AttemptAttributeFloatRescale(XElement element, string attributeName, float multiplier)
		{
			var xAttribute = element.Attribute(attributeName);
			if (xAttribute == null) return;

			var value = GetFloatValueFromAttribute(xAttribute);
			if (value == null) return;

			var scaledValue = value.Value * multiplier;

			var roundedScaledValue = Math.Round(scaledValue, 2);

			xAttribute.Value = roundedScaledValue.ToString(CultureInfo.InvariantCulture);
		}

		private float? GetFloatValueFromAttribute(XAttribute xAttribute)
		{
			if (float.TryParse(xAttribute.Value, out var floatValue))
			{
				return floatValue;
			}

			return null;
		}
	}
}
