using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// Serialization for AltCurve
	/// </summary>
	public class JsonConverter : JsonConverter<AltCurve>
	{
		public const uint SERIALIZE_VERSION = 1;
		private const string TokenName_Version = "_ace_v";
		private const string TokenName_PreInfinity = "pri";
		private const string TokenName_PostInfinity = "poi";
		private const string TokenName_Keys = "keys";

		public override AltCurve Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
		{
			if ( reader.TokenType != JsonTokenType.StartObject )
				throw new JsonException();

			uint version = 0;
			Extrapolation preInfinity = 0;
			Extrapolation postInfinity = 0;
			List<Keyframe> keyframes = new();

			while ( reader.Read() )
			{
				if ( reader.TokenType == JsonTokenType.EndObject )
				{
					if ( version != SERIALIZE_VERSION )
						throw new JsonException( $"Error parsing AltCurve json, unsupported version: {version} vs {SERIALIZE_VERSION}" );

#if DEBUG
					// Warn in debug mode if the sanitized keyframe differs from what we tried to load
					var sanitizedKeyframes = SanitizeKeyframes( keyframes );
					if ( !sanitizedKeyframes.SequenceEqual( keyframes ) )
					{
						// Notify of keyframe removals
						foreach ( var removedKey in keyframes.Except( sanitizedKeyframes ) )
						{
							Log.Error( $"Discarded invalid AltCurve keyframe JSON, no keyframes in a curve may share a time. Removed keyframe: '{removedKey}'." );
						}
					
						// Check if remaining keyframes are in the same order (intersection should be equal to sanitized keyframes)
						if ( !keyframes.Intersect( sanitizedKeyframes ).SequenceEqual( sanitizedKeyframes ) )
						{
							Log.Error( "AltCurve keyframe JSON out-of-order, performed fixup." );
						}

						keyframes = sanitizedKeyframes.ToList();
					}
#endif

					return new( keyframes, preInfinity, postInfinity );
				}

				if ( reader.TokenType == JsonTokenType.PropertyName )
				{
					string propertyName = reader.GetString();
					reader.Read();

					switch ( propertyName )
					{
						case TokenName_Version:
							version = reader.GetUInt32();
							break;
						case TokenName_PreInfinity:
							preInfinity = JsonSerializer.Deserialize<AltCurve.Extrapolation>( ref reader, options );
							break;
						case TokenName_PostInfinity:
							postInfinity = JsonSerializer.Deserialize<AltCurve.Extrapolation>( ref reader, options );
							break;
						case TokenName_Keys:
							keyframes = JsonSerializer.Deserialize<List<Keyframe>>( ref reader, options );
							break;
						default:
							throw new JsonException( $"Unknown property: {propertyName}" );
					}
				}
			}

			throw new JsonException( "Unexpected end of JSON." );
		}

		public override void Write( Utf8JsonWriter writer, AltCurve val, JsonSerializerOptions options )
		{
			writer.WriteStartObject();
			writer.WritePropertyName( TokenName_Version );
			writer.WriteNumberValue( SERIALIZE_VERSION );

			if ( val.PreInfinity != default( AltCurve ).PreInfinity )
			{
				writer.WritePropertyName( TokenName_PreInfinity );
				JsonSerializer.Serialize( writer, val.PreInfinity, options );
			}

			if ( val.PostInfinity != default( AltCurve ).PostInfinity )
			{
				writer.WritePropertyName( TokenName_PostInfinity );
				JsonSerializer.Serialize( writer, val.PostInfinity, options );
			}

			if ( !val.Keyframes.IsDefaultOrEmpty )
			{
				writer.WritePropertyName( TokenName_Keys );
				JsonSerializer.Serialize( writer, val.Keyframes, options );
			}

			writer.WriteEndObject();
		}
	}
}

