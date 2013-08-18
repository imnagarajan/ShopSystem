﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wolfje.Plugins.SEconomy {
    internal class ChatCommands {

        /// <summary>
        /// Hooks to chat commands.
        /// </summary>
        public static void Initialize() {
            TShockAPI.Commands.ChatCommands.Add(new TShockAPI.Command(Chat_BankCommand, "bank") { AllowServer = true });
        }

        static void Chat_BankCommand(TShockAPI.CommandArgs args) {
            //The initator of the command with bank account...
            Economy.EconomyPlayer selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Player.Index);
            Economy.EconomyPlayer caller = SEconomyPlugin.GetEconomyPlayerSafe(args.Player.Index);

            string namePrefix = "Your";

            if (args.Parameters.Count == 0) {
                args.Player.SendInfoMessageFormat("This server is running SEconomy v{0}", SEconomyPlugin.PluginVersion);
                args.Player.SendInfoMessage("You can:");

                args.Player.SendInfoMessage("* View your balance with /bank bal");

                if (args.Player.Group.HasPermission("bank.transfer")) {
                    args.Player.SendInfoMessage("* Trade players with /bank pay <player> <amount>");
                }

                if (args.Player.Group.HasPermission("bank.viewothers")) {
                    args.Player.SendInfoMessage("* View other people's balance with /bank bal <player>");
                }

                if (args.Player.Group.HasPermission("bank.worldtransfer")) {
                    args.Player.SendInfoMessage("* Spawn/delete money with /bank give|take <player> <amount>");
                }

                if (args.Player.Group.HasPermission("bank.mgr")) {
                    args.Player.SendInfoMessage("* Spawn the account manager GUI on the server with /bank mgr");
                }

                if (args.Player.Group.HasPermission("bank.savejournal")) {
                    args.Player.SendInfoMessage("* Save the journal with /bank savejournal");
                }

                if (args.Player.Group.HasPermission("bank.loadjournal")) {
                    args.Player.SendInfoMessage("* Load the journal with /bank loadjournal");
                }

                if (args.Player.Group.HasPermission("bank.squashjournal")) {
                    args.Player.SendInfoMessage("* Compress the journal with /bank squashjournal");
                }

                return;
            }

            //Bank balance
            if (args.Parameters[0].Equals("bal", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("balance", StringComparison.CurrentCultureIgnoreCase)) {


                //The command supports viewing other people's balance if the caller has permission
                if (args.Player.Group.HasPermission("bank.viewothers")) {
                    if (args.Parameters.Count >= 2) {
                        selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                    }

                    if (selectedPlayer != null) {
                        namePrefix = selectedPlayer.TSPlayer.Name + "'s";
                    }
                }

                if (selectedPlayer != null && selectedPlayer.BankAccount != null) {

                    if (!selectedPlayer.BankAccount.IsAccountEnabled && !args.Player.Group.HasPermission("bank.viewothers")) {
                        args.Player.SendErrorMessage("bank balance: your account is disabled");
                    } else {
                        args.Player.SendInfoMessageFormat("{1} balance: {0} {2}", selectedPlayer.BankAccount.Balance.ToLongString(true), namePrefix, selectedPlayer.BankAccount.IsAccountEnabled ? "" : "(disabled)");
                    }

                } else {
                    args.Player.SendInfoMessage("bank balance: Cannot find player or no bank account.");
                }
            } else if (args.Parameters[0].Equals("mgr")) {
                if (args.Player.Group.HasPermission("bank.mgr")) {

                    if (args.Player is TShockAPI.TSServerPlayer) {
                        Forms.CAccountManagementWnd wnd = new Forms.CAccountManagementWnd();

                        Task.Factory.StartNew(() => {
                            TShockAPI.Log.ConsoleInfo("seconomy management: opening bank manager window");

                            //writing the journal is not possible when you're fucking with it in the manager
                            //last thing you want is for half baked changes to be pushed to disk
                            SEconomyPlugin.BackupCanRun = false;

                            wnd.ShowDialog();
                        }, creationOptions: TaskCreationOptions.LongRunning).ContinueWith((task) => {

                            SEconomyPlugin.BackupCanRun = true;

                            TShockAPI.Log.ConsoleInfo("seconomy management: window closed");
                            Journal.TransactionJournal.BackupJournalAsync();
                        });
                    } else {
                        args.Player.SendErrorMessage("Only the console can do that.");
                    }
                }

            } else if (args.Parameters[0].Equals("savejournal")) {
                if (args.Player.Group.HasPermission("bank.savejournal")) {
                    args.Player.SendInfoMessage("seconomy xml: Backing up transaction journal.");

                    Journal.TransactionJournal.SaveXml(Configuration.JournalPath);
                }

            } else if (args.Parameters[0].Equals("loadjournal")) {
                if (args.Player.Group.HasPermission("bank.loadjournal")) {
                    args.Player.SendInfoMessage("seconomy xml: Loading transaction journal from file");

                    Journal.TransactionJournal.LoadFromXmlFile(Configuration.JournalPath);
                }

            } else if (args.Parameters[0].Equals("squashjournal", StringComparison.CurrentCultureIgnoreCase)) {
                if (args.Player.Group.HasPermission("bank.squashjournal")) {
                    Guid p = SEconomyPlugin.Profiler.Enter("Squash journal");
                    Journal.TransactionJournal.SquashJournalAsync().ContinueWith((task) => {
                        Journal.TransactionJournal.SaveXml(Configuration.JournalPath);

                        SEconomyPlugin.Profiler.ExitLog(p);
                    });
                } else {
                    args.Player.SendErrorMessage("bank squashjournal: You do not have permission to perform this command.");
                }
            } else if (args.Parameters[0].Equals("listbal", StringComparison.CurrentCultureIgnoreCase)) {
                //Admin command: lists people's balances
                if (args.Player.Group.HasPermission("bank.listbal")) {
                    int takeFrom = 0, takeTo = 25, page = 1;

                    if (args.Parameters.Count >= 2 && int.TryParse(args.Parameters[1], out page)) {
                        takeFrom = page * 25;
                        takeTo = takeFrom + 25;
                    } 

                    args.Player.SendInfoMessage("Bank Balances - Page " + page);
                    args.Player.SendInfoMessage("===");

                    foreach (Journal.XBankAccount bankAccount in Journal.TransactionJournal.BankAccounts.Skip(takeFrom).Take(25)) {
                        bankAccount.SyncBalanceAsync().ContinueWith((task) => {
                            args.Player.SendInfoMessageFormat("* {0} : {1}", bankAccount.UserAccountName, bankAccount.Balance);
                        });
                    }            
                }

            } else if (args.Parameters[0].Equals("ena", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("enable", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("dis", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("disable", StringComparison.CurrentCultureIgnoreCase)) {
                //Account enable

                //Flag to set the account to
                bool enableAccount = args.Parameters[0].Equals("ena", StringComparison.CurrentCultureIgnoreCase) || args.Parameters[0].Equals("enable", StringComparison.CurrentCultureIgnoreCase);

                if (args.Player.Group.HasPermission("bank.modifyothers")) {
                    if (args.Parameters.Count >= 2) {
                        selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                    }

                    if (selectedPlayer != null) {
                        namePrefix = selectedPlayer.TSPlayer.Name + "'s";
                    }
                }

                if (selectedPlayer != null && selectedPlayer.BankAccount != null) {
                    selectedPlayer.BankAccount.SetAccountEnabled(args.Player.Index, enableAccount);
                }
            } else if (args.Parameters[0].Equals("pay", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("transfer", StringComparison.CurrentCultureIgnoreCase)
                || args.Parameters[0].Equals("tfr", StringComparison.CurrentCultureIgnoreCase)) {
                //Player-to-player transfer
            
                if (selectedPlayer.TSPlayer.Group.HasPermission("bank.transfer")) {
                    // /bank pay wolfje 1p
                    if (args.Parameters.Count >= 3) {
                        selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                        Money amount = 0;

                        if (selectedPlayer == null) {
                            args.Player.SendErrorMessageFormat("Cannot find player by the name of {0}.", args.Parameters[1]);
                        } else {
                            if (Money.TryParse(args.Parameters[2], out amount)) {

                                //Instruct the world bank to give the player money.
                                caller.BankAccount.TransferTo(selectedPlayer.BankAccount, amount, Journal.BankAccountTransferOptions.AnnounceToReceiver | Journal.BankAccountTransferOptions.AnnounceToSender | Journal.BankAccountTransferOptions.IsPlayerToPlayerTransfer, Message: string.Format("SE: tfr: {0} to {1} for {2}", caller.TSPlayer.Name, selectedPlayer.TSPlayer.Name, amount.ToString()));
                            } else {
                                args.Player.SendErrorMessageFormat("bank give: \"{0}\" isn't a valid amount of money.", args.Parameters[2]);
                            }
                        }
                    } else {
                        args.Player.SendErrorMessage("Usage: /bank pay <Player> <Amount>");
                    }
                } else {
                    args.Player.SendErrorMessageFormat("bank pay: You don't have permission to do that.");
                }

            } else if (args.Parameters[0].Equals("give", StringComparison.CurrentCultureIgnoreCase)
               || args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase)) {
                //World-to-player transfer
            
                if (selectedPlayer.TSPlayer.Group.HasPermission("bank.worldtransfer")) {
                    // /bank give wolfje 1p
                    if (args.Parameters.Count >= 3) {
                        selectedPlayer = SEconomyPlugin.GetEconomyPlayerSafe(args.Parameters[1]);
                        Money amount = 0;

                        if (selectedPlayer == null) {
                            args.Player.SendErrorMessageFormat("Cannot find player by the name of {0}.", args.Parameters[1]);
                        } else {
                            if (Money.TryParse(args.Parameters[2], out amount)) {

                                //eliminate a double-negative.  saying "take Player -1p1c" will give them 1 plat 1 copper!
                                if (args.Parameters[0].Equals("take", StringComparison.CurrentCultureIgnoreCase) && amount > 0) {
                                    amount = -amount;
                                }

                                //Instruct the world bank to give the player money.
                                SEconomyPlugin.WorldAccount.TransferTo(selectedPlayer.BankAccount, amount, Journal.BankAccountTransferOptions.AnnounceToReceiver, Message: string.Format("SE: pay: {0} to {1} ", amount.ToString(), selectedPlayer.TSPlayer.Name));
                            } else {
                                args.Player.SendErrorMessageFormat("bank give: \"{0}\" isn't a valid amount of money.", args.Parameters[2]);
                            }
                        }
                    } else {
                        args.Player.SendErrorMessage("Usage: /bank give|take <Player> <Amount");
                    }
                } else {
                    args.Player.SendErrorMessageFormat("bank give: You don't have permission to do that.");
                }
            }
        }
        
    }
}
