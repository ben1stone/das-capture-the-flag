﻿using DAS_Capture_The_Flag.Models.Game;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace DAS_Capture_The_Flag.Hubs
{

    public interface IGameClient
    {
        Task StartGame(string gameId, string playerId);
        Task PlayerReady(bool playerOne, bool playerTwo);
        Task AwaitPlayersReady();
        Task UpdatePlayerReady();
        Task OpponentReady();
        Task OpponentLeftLobby();
    }

    public class GameSetupHub : Hub<IGameClient>
    {

        private IGameRepository _repository;

        public GameSetupHub(IGameRepository repository)
        {
            _repository = repository;
        }

        public override async Task OnConnectedAsync()
        {
            var game = _repository.Games.FirstOrDefault(g => !g.Setup.PlayersConnected);

            if (game == null)
            {
                game = new Game();
                game.Setup = new GameSetup();

                _repository.Games.Add(game);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, game.Setup.GameId);

            if (game.Setup.Players[0].ConnectionId == null)
            {
                game.Setup.Players[0].ConnectionId = Context.ConnectionId;
                game.Setup.PlayersConnected = GetPlayersConnected(game.Setup);
            }
            else
            {
                game.Setup.Players[1].ConnectionId = Context.ConnectionId;
                game.Setup.PlayersConnected = GetPlayersConnected(game.Setup);
            }

            await Clients.Group(game.Setup.GameId)
                .PlayerReady(game.Setup.Players[0].ConnectionId != null, game.Setup.Players[1].ConnectionId != null);

            await base.OnConnectedAsync();


            if (game.Setup.PlayersConnected)
            {
                await Clients.Group(game.Setup.GameId).AwaitPlayersReady();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Handle Disconnections
            Game game = (Game)_repository.Games.FirstOrDefault(g => g.Setup.Players[0].ConnectionId == Context.ConnectionId || g.Setup.Players[1].ConnectionId == Context.ConnectionId);

            Player player = game.Setup.Players.Where(x => x.ConnectionId == Context.ConnectionId).FirstOrDefault();
            Player opponent = game.Setup.Players.Where(x => x.ConnectionId != Context.ConnectionId).FirstOrDefault();

            player.ConnectionId = null;
            player.Ready = false;
            player.Name = null;
            game.Setup.PlayersConnected = false;

            await Clients.Client(opponent.ConnectionId).OpponentLeftLobby();
            await Clients.Group(game.Setup.GameId).PlayerReady(game.Setup.Players[0].ConnectionId != null, game.Setup.Players[1].ConnectionId != null);


        }

        private Player GetPlayerFromId(Game game, string connectionId)
        {
            if (game.Setup.Players[0].ConnectionId == connectionId)
            {
                return game.Setup.Players[0];
            }
            else
            {
                return game.Setup.Players[1];
            }
        }

        public async Task UpdatePlayerReady()
        {
            var repo = _repository;
            var game = _repository.Games.FirstOrDefault(g => g.Setup.HasPlayer(Context.ConnectionId));

            var player = GetPlayer(game.Setup, Context.ConnectionId);

            player.Ready = true;

            if (GetPlayersReady(game.Setup))
            {
                await Clients.Client(game.Setup.Players[0].ConnectionId).StartGame(game.Setup.GameId.ToString(), game.Setup.Players[0].ConnectionId.ToString());
                await Clients.Client(game.Setup.Players[1].ConnectionId).StartGame(game.Setup.GameId.ToString(), game.Setup.Players[1].ConnectionId.ToString());

            }
            else
            {
                var otherPlayer = GetOpponent(game.Setup, Context.ConnectionId);
                await Clients.Client(otherPlayer.ConnectionId).OpponentReady();
            }
        }

       
        private bool GetPlayersConnected(GameSetup game)
        {
            if (game.Players[0].ConnectionId != null && game.Players[1].ConnectionId != null)
            {
                return true;
            }

            return false;
        }

        private bool GetPlayersReady(GameSetup game)
        {
            if (game.Players[0].Ready && game.Players[1].Ready)
            {
                return true;
            }

            return false;
        }

        private Player GetPlayer(GameSetup game, string id)
        {
            if (game.Players[0].ConnectionId == id)
            {
                return game.Players[0];
            }

            return game.Players[1];
        }

        private Player GetOpponent(GameSetup game, string id)
        {
            if (game.Players[0].ConnectionId != id)
            {
                return game.Players[0];
            }

            return game.Players[1];
        }
    }
}
