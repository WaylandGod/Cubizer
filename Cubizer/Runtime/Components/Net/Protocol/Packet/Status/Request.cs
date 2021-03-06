﻿using Cubizer.Net.Protocol.Serialization;

namespace Cubizer.Net.Protocol.Status.Serverbound
{
	[Packet(Packet)]
	public sealed class Request : IPacketSerializable
	{
		public const int Packet = 0x00;

		public uint packetId
		{
			get
			{
				return packetId;
			}
		}

		public void Deserialize(NetworkReader br)
		{
		}

		public void Serialize(NetworkWrite bw)
		{
		}

		public object Clone()
		{
			return new Request();
		}
	}
}