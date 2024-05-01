using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HypeRate
{
    class Channels
    {
        private readonly Dictionary<int, string> joiningChannels = new();

        private readonly List<string> joinedChannels = new();

        private readonly Dictionary<int, string> leavingChannels = new();

        private readonly Random randomNumberGenerator = new();

        private readonly List<int> refsInUse = new();

        private int GenerateRandomRef()
        {
            var generatedRef = randomNumberGenerator.Next(1, int.MaxValue - 1);

            while (refsInUse.Contains(generatedRef))
            {
                generatedRef = randomNumberGenerator.Next(1, int.MaxValue - 1);
            }

            return generatedRef;
        }

        public RefType DetermineRefType(int @ref)
        {
            if (joiningChannels.ContainsKey(@ref))
            {
                return RefType.Join;
            }

            if (leavingChannels.ContainsKey(@ref))
            {
                return RefType.Leave;
            }

            return RefType.Unknown;
        }

        /// <summary>
        /// Returns -1 when the given channel name has already been joined or is about to be joined.
        /// Removes the channel name from the leaving channels when the channel name is about to be left.
        /// </summary>
        /// <param name="channelName">The name of the channel to join</param>
        /// <returns>
        /// Returns -1 when the given channel name has already been joined or is about to be joined.
        /// Otherwise returns the generated ref for the join command.
        /// </returns>
        public int AddJoiningChannel(string channelName)
        {
            if (joinedChannels.Contains(channelName))
            {
                return -1;
            }

            if (joiningChannels.ContainsValue(channelName))
            {
                return -1;
            }

            if (leavingChannels.ContainsValue(channelName))
            {
                var leavingChannelRef = leavingChannels.First((pair) => pair.Value == channelName).Key;
                leavingChannels.Remove(leavingChannelRef);
                refsInUse.Remove(leavingChannelRef);
            }

            var generatedRef = GenerateRandomRef();

            joiningChannels.Add(generatedRef, channelName);
            refsInUse.Add(generatedRef);

            return generatedRef;
        }

        public void HandleChannelJoin(int joinedRef)
        {
            if (refsInUse.Contains(joinedRef) == false)
            {
                return;
            }

            var channelName = joiningChannels[joinedRef];

            if (channelName == null)
            {
                return;
            }

            refsInUse.Remove(joinedRef);
            joiningChannels.Remove(joinedRef);
            joinedChannels.Add(channelName);
        }

        public int AddLeavingChannel(string channelName)
        {
            var generatedRef = GenerateRandomRef();

            leavingChannels.Add(generatedRef, channelName);
            refsInUse.Add(generatedRef);

            return generatedRef;
        }

        public List<string> GetChannelsToJoin()
        {
            var result = new List<string>();
            var channelsToLeave = leavingChannels.Values.ToList();

            foreach (var joinedChannel in joinedChannels)
            {
                if (channelsToLeave.Contains(joinedChannel))
                {
                    continue;
                }

                result.Add(joinedChannel);
            }

            foreach (var joiningChannel in joiningChannels.Values)
            {
                if (channelsToLeave.Contains(joiningChannel))
                {
                    continue;
                }

                result.Add(joiningChannel);
            }

            return result;
        }

        public void HandleJoin(int @ref)
        {
            if (refsInUse.Contains(@ref) == false)
            {
                return;
            }

            var joinedChannelName = joiningChannels[@ref];

            refsInUse.Remove(@ref);
            joiningChannels.Remove(@ref);
            joinedChannels.Add(joinedChannelName);
        }

        public void HandleLeave(int @ref)
        {
            if (refsInUse.Contains(@ref) == false)
            {
                return;
            }

            var leftChannelName = leavingChannels[@ref];

            if (leftChannelName == null)
            {
                return;
            }

            refsInUse.Remove(@ref);
            leavingChannels.Remove(@ref);
        }

        public void HandleReconnect()
        {
            joiningChannels.Clear();
            leavingChannels.Clear();
            joinedChannels.Clear();
            refsInUse.Clear();
        }

        public override string ToString()
        {
            return String.Format(
                "Channels {{ joining: [ {0} ], joined: [ {1} ], leaving: [ {2} ]}}",
                string.Join(", ", joiningChannels.Values),
                string.Join(", ", joinedChannels),
                string.Join(", ", leavingChannels.Values)
            );
        }
    }
}