using AStarNavigator.Algorithms;
using AStarNavigator.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AStarNavigator
{
    public class BidirectionalTileNavigator : ITileNavigator
    {
        private readonly IBlockedProvider blockedProvider;
        private readonly INeighborProvider neighborProvider;

        private readonly IDistanceAlgorithm distanceAlgorithm;
        private readonly IDistanceAlgorithm heuristicAlgorithm;

        //StreamWriter logger = new StreamWriter("shit_" + DateTime.Now.GetHashCode() + ".log");

        public BidirectionalTileNavigator(
            IBlockedProvider blockedProvider,
            INeighborProvider neighborProvider,
            IDistanceAlgorithm distanceAlgorithm,
            IDistanceAlgorithm heuristicAlgorithm)
        {
            this.blockedProvider = blockedProvider;
            this.neighborProvider = neighborProvider;

            this.distanceAlgorithm = distanceAlgorithm;
            this.heuristicAlgorithm = heuristicAlgorithm;
        }

        public IEnumerable<Tile> Navigate(Tile from, Tile to)
        {
            //logger.WriteLine("CALL " + from.X + " " + from.Y + ":" + to.X + " " + to.Y);
            var closedFrom = new List<Tile>();
            var openFrom = new List<Tile>() { from };
            var closedTo = new List<Tile>();
            var openTo = new List<Tile>() { to };


            var pathFrom = new Dictionary<Tile, Tile>();
            var pathTo = new Dictionary<Tile, Tile>();

            var gScoreFrom = new Dictionary<Tile, double>();
            var gScoreTo = new Dictionary<Tile, double>();
            gScoreFrom[from] = 0;
            gScoreTo[to] = 0;

            var fScoreFrom = new Dictionary<Tile, double>();
            var fScoreTo = new Dictionary<Tile, double>();
            fScoreFrom[from] = heuristicAlgorithm.Calculate(from, to);
            fScoreTo[to] = heuristicAlgorithm.Calculate(to, from);

            while (openFrom.Any() && openTo.Any())
            {
                var currentFrom = openFrom
                    .OrderBy(c => fScoreFrom[c])
                    .First();
                var currentTo = openTo
                    .OrderBy(c => fScoreTo[c])
                    .First();

                //var temp = open
                //    .OrderBy(c => fScore[c]);

                //var current = temp.First();


                //logger.WriteLine("current " + current.X + " " + current.Y);
                //foreach (var item in temp)
                //{
                //    logger.WriteLine("ordered " + +item.X + " " + item.Y);
                //}
                //foreach (var item in fScore)
                //{
                //    logger.WriteLine("fscore " +item.Key.X + " " + item.Key.Y + " " + item.Value);
                //}

                if (currentTo == currentFrom || closedFrom.Contains(currentTo) || closedTo.Contains(currentFrom))
                {
                    return ReconstructPath(ReconstructPathFrom(pathFrom, currentFrom), ReconstructPathTo(pathTo, currentTo));
                }

                openFrom.Remove(currentFrom);
                openTo.Remove(currentTo);
                closedFrom.Add(currentFrom);
                closedTo.Add(currentTo);

                foreach (Tile neighbor in neighborProvider.GetNeighbors(currentFrom))
                {
                    if (closedFrom.Contains(neighbor) || blockedProvider.IsBlocked(neighbor))
                    {
                        continue;
                    }

                    var tentativeG = gScoreFrom[currentFrom] + distanceAlgorithm.Calculate(currentFrom, neighbor);

                    if (!openFrom.Contains(neighbor))
                    {
                        openFrom.Add(neighbor);
                    }
                    else if (tentativeG >= gScoreFrom[neighbor])
                    {
                        continue;
                    }

                    pathFrom[neighbor] = currentFrom;

                    gScoreFrom[neighbor] = tentativeG;
                    fScoreFrom[neighbor] = gScoreFrom[neighbor] + heuristicAlgorithm.Calculate(neighbor, to);
                }

                foreach (Tile neighbor in neighborProvider.GetNeighbors(currentTo))
                {
                    if (closedTo.Contains(neighbor) || blockedProvider.IsBlocked(neighbor))
                    {
                        continue;
                    }

                    var tentativeG = gScoreTo[currentTo] + distanceAlgorithm.Calculate(currentTo, neighbor);

                    if (!openTo.Contains(neighbor))
                    {
                        openTo.Add(neighbor);
                    }
                    else if (tentativeG >= gScoreTo[neighbor])
                    {
                        continue;
                    }

                    pathTo[neighbor] = currentTo;

                    gScoreTo[neighbor] = tentativeG;
                    fScoreTo[neighbor] = gScoreTo[neighbor] + heuristicAlgorithm.Calculate(neighbor, to);
                }
            }

            return null;
        }

        private IEnumerable<Tile> ReconstructPathFrom(
            IDictionary<Tile, Tile> path,
            Tile current)
        {
            List<Tile> totalPath = new List<Tile>() { current };

            while (path.ContainsKey(current))
            {
                current = path[current];
                totalPath.Add(current);
            }

            totalPath.Reverse();
            totalPath.RemoveAt(0);

            return totalPath;
        }

        private IEnumerable<Tile> ReconstructPathTo(
            IDictionary<Tile, Tile> path,
            Tile current)
        {
            List<Tile> totalPath = new List<Tile>() { current };

            while (path.ContainsKey(current))
            {
                current = path[current];
                totalPath.Add(current);
            }

            totalPath.RemoveAt(0);

            return totalPath;
        }

        private IEnumerable<Tile> ReconstructPath(
            IEnumerable<Tile> pathFrom,
            IEnumerable<Tile> pathTo)
        {
            return pathFrom.Concat(pathTo);
        }
    }
}
