﻿using System;
using System.Collections.Generic;
using fCraft.Collections;
using System.Linq;
using System.Text;
using fCraft.Physics;
using System.Threading;
using fCraft.Events;
using fCraft.Drawing;

namespace fCraft.Physics
{
    class ExplodingPhysics
    {
        private static Thread explodeThread;

        public static void TNTClick(object sender, Events.PlayerClickedEventArgs e)
        {
            World world = e.Player.World;
            if (world.Map.GetBlock(e.Coords) == Block.TNT)
            {
                lock (world.SyncRoot)
                {
                    world.Map.QueueUpdate(new BlockUpdate(null, e.Coords, Block.Air));
                    int Seed = new Random().Next(1, 50);
                    world._physScheduler.AddTask(new TNTTask(world, e.Coords, e.Player), 0);
                }
            }
        }

        public static void Firework(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            try
            {
                World world = e.Player.World;
                if (world.fireworkPhysics)
                {
                    if (world.Map != null && world.IsLoaded)
                    {
                        if (e.Context == BlockChangeContext.Manual)
                        {
                            if (e.NewBlock == Block.Gold)
                            {
                                if (e.Player.fireworkMode)
                                {
                                    if (world.FireworkCount >= 10)
                                    {
                                        e.Player.Message("Failed to launch: Too many fireworks active");
                                        e.Result = CanPlaceResult.Revert;
                                        return;
                                    }
                                    explodeThread = new Thread(new ThreadStart(delegate
                                    {
                                        lock (world.SyncRoot)
                                        {
                                            world.FireworkCount++;
                                            int upZ = e.Coords.Z;
                                            int height = new Random().Next(12, 25);
                                            for (int up = 0; up <= height; up++)
                                            {
                                                if (world.Map != null && world.IsLoaded)
                                                {
                                                    Thread.Sleep(Physics.Tick); //world check after every thread sleep
                                                    if (world.Map != null && world.IsLoaded) //dis
                                                    {
                                                        if (!Physics.BlockThrough(world.Map.GetBlock(e.Coords.X, e.Coords.Y, upZ + 1)))
                                                        {
                                                            Thread.Sleep(1000);
                                                            break;
                                                        }
                                                    }
                                                    if (world.Map != null && world.IsLoaded)
                                                    {
                                                        upZ++;
                                                        if (upZ == e.Coords.Z)
                                                        {
                                                            return;
                                                        }
                                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, (upZ - 2)) == Block.Lava)
                                                        {
                                                            world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(upZ - 2), Block.Air));
                                                        }
                                                        if (world.Map.GetBlock(e.Coords.X, e.Coords.Y, (upZ - 1)) == Block.Gold)
                                                        {
                                                            world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(upZ - 1), Block.Lava));
                                                        }
                                                        world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)upZ, Block.Gold));
                                                    }
                                                }
                                            }
                                            int X2, Y2, Z2;

                                            Random rand = new Random();
                                            int blockId = rand.Next(1, 9);
                                            Block fBlock = new Block();
                                            if (blockId == 1)
                                            {
                                                fBlock = Block.Lava;
                                            }
                                            if (blockId <= 6 && blockId != 1)
                                            {
                                                fBlock = (Block)rand.Next(21, 33);
                                            }
                                            if (world.Map != null && world.IsLoaded)
                                            {
                                                world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)upZ, Block.Air));
                                                world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)e.Coords.Y, (short)(upZ - 1), Block.Air));
                                            }
                                            for (X2 = e.Coords.X - (Physics.size + 1); X2 <= e.Coords.X + (Physics.size + 1); X2++)
                                            {
                                                for (Y2 = (e.Coords.Y - (Physics.size + 1)); Y2 <= (e.Coords.Y + (Physics.size + 1)); Y2++)
                                                {
                                                    for (Z2 = (upZ - (Physics.size + 1)); Z2 <= (upZ + (Physics.size + 1)); Z2++)
                                                    {
                                                        if (rand.Next(1, 50) < 3)
                                                        {
                                                            if (world.Map != null && world.IsLoaded)
                                                            {
                                                                if (!Physics.BlockThrough(world.Map.GetBlock(X2, Y2, Z2)))
                                                                {
                                                                    break;
                                                                }
                                                                if (blockId >= 7)
                                                                {
                                                                    fBlock = (Block)rand.Next(21, 33);
                                                                }
                                                                if (world.Map != null && world.IsLoaded)
                                                                {
                                                                    if (world.fireworkPhysics)
                                                                    {
                                                                        Explode(world, X2, Y2, Z2, (Block)fBlock);
                                                                        Removal(world, X2, Y2, Z2);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            world.FireworkCount--;
                                        }
                                    })); explodeThread.Start();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }

        public static void Explode(World w, int X2, int Y2, int Z2, Block block)
        {
            if (w.Map != null && w.IsLoaded)
            {
                BlockUpdate fwSender = new BlockUpdate(null,
                    (short)X2,
                    (short)Y2,
                    (short)Z2,
                    block);
                w.Map.QueueUpdate(fwSender);
            }
        }

        public static void Removal(World w, int X2, int Y2, int Z2)
        {
            if (w.Map != null && w.IsLoaded)
            {
                BlockUpdate fwRemover = new BlockUpdate(null,
                    (short)X2,
                    (short)Y2,
                    (short)Z2,
                    Block.Air);
                Scheduler.NewTask(t => send(w, fwRemover)).RunOnce(TimeSpan.FromMilliseconds(300));
            }
        }
        //cunts
        public static void send(World w, BlockUpdate fwRemover)
        {
            if (w.Map != null && w.IsLoaded)
            {
                w.Map.QueueUpdate(fwRemover);
            }
        }
    }
}
