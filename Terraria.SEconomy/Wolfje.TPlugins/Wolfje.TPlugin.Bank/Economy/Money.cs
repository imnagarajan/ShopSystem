﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Wolfje.Plugins.SEconomy {
    /// <summary>
    /// A representation of Money in Seconomy.  Money objects are toll-free bridged with 64-bit integers (long).
    /// </summary>
    public struct Money {
        static readonly object __staticLock = new object();

        //ye olde godly value base type
        private long _moneyValue;

        private const int ONE_PLATINUM = 1000000;
        private const int ONE_GOLD = 10000;
        private const int ONE_SILVER = 100;
        private const int ONE_COPPER = 1;

        private static readonly Regex moneyRegex = new Regex(string.Format(@"(-)?((\d*){0})?((\d*){1})?((\d*){2})?((\d*){3})?", SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant4Abbreviation,
            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant3Abbreviation, 
            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant2Abbreviation, 
            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant1Abbreviation), RegexOptions.IgnoreCase);

        private static readonly Regex numberRegex = new Regex(@"(\d*)", RegexOptions.IgnoreCase);

        #region "Constructors"

        /// <summary>
        /// Initializes a new instance of Money and copies the amount specified in it.
        /// </summary>
        public Money(Money money) {
            _moneyValue = money._moneyValue;
        }

        /// <summary>
        /// Initalizes a new instance of money with the specified amount in integer form.
        /// </summary>
        public Money(long money) {
            _moneyValue = money;
        }

        /// <summary>
        /// Makes a new money object based on the supplied platinum, gold, silver, and copper.
        /// </summary>
        public Money(uint Platinum, uint Gold, int Silver, int Copper) {
            _moneyValue = 0;

            if (Gold > 99 || Silver > 99 || Copper > 99) {
                throw new ArgumentException("Supplied values for Gold, silver and copper cannot be over 99.");
            } else {
                _moneyValue += (long)Math.Pow(Platinum, 6);
                _moneyValue += (long)Math.Pow(Gold, 4);
                _moneyValue += (long)Math.Pow(Silver, 2);
                _moneyValue += (long)Copper;
            }
        }

        #endregion

        #region "Long toll-free bridging"

        /// <summary>
        /// Cast a Long to Money implicitly
        /// </summary>
        public static implicit operator Money(long money) {
            return new Money(money);
        }

        /// <summary>
        /// Cast Money to a Long implicitly
        /// </summary>
        public static implicit operator long(Money money) {
            return money._moneyValue;
        }

        #endregion
        
        /// <summary>
        /// Returns the Platinum portion of this money instance
        /// </summary>
        public long Platinum {
            get {
                lock (__staticLock) {
                    if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                        return (long)Math.Floor((decimal)(_moneyValue / ONE_PLATINUM));
                    } else {
                        return _moneyValue;
                    }
                    
                }
            }
        }

        /// <summary>
        /// Returns the Gold portion of this money instance
        /// </summary>
        public long Gold {
            get {
                lock (__staticLock) {
                    if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                        return (long)((_moneyValue % ONE_PLATINUM) - (_moneyValue % ONE_GOLD)) / 10000;
                    } else {
                        return _moneyValue;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the Silver portion of this money instance
        /// </summary>
        public long Silver {
            get {
                lock (__staticLock) {
                    if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                        return (long)((_moneyValue % ONE_GOLD) - (_moneyValue % ONE_SILVER)) / 100;
                    } else {
                        return _moneyValue;
                    }
                }
            }
        }
        

        /// <summary>
        /// Returns the Copper portion of this money instance
        /// </summary>
        public long Copper {
            get {
                lock (__staticLock) {
                    if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                        return (long)_moneyValue % 100;
                    } else {
                        return _moneyValue;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the raw value of this structure.
        /// </summary>
        public long Value {
            get {
                return _moneyValue;
            }
        }

        /// <summary>
        /// Returns the string representation of this money
        /// </summary>
        public override string ToString() {
            lock (__staticLock) {

                StringBuilder sb = new StringBuilder();

                if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                    Money moneyCopy = this;

                    //Negative balances still need to display like they are positives
                    if (moneyCopy < 0) {
                        sb.Append("-");
                        moneyCopy = moneyCopy * (-1);
                    }

                    if (moneyCopy.Platinum > 0) {
                        sb.AppendFormat("{0}{1}", moneyCopy.Platinum, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant4Abbreviation);
                    }
                    if (moneyCopy.Gold > 0) {
                        sb.AppendFormat("{0}{1}", moneyCopy.Gold, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant3Abbreviation);
                    }
                    if (moneyCopy.Silver > 0) {
                        sb.AppendFormat("{0}{1}", moneyCopy.Silver, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant2Abbreviation);
                    }

                    if (moneyCopy.Copper > 0) {
                        sb.AppendFormat("{0}{1}", moneyCopy.Copper, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant1Abbreviation);
                    } else if (((long)moneyCopy) == 0) {
                        sb.AppendFormat("{0}{1}", moneyCopy.Copper, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant1Abbreviation);
                    }

                } else {
                    //Used in singular format: produces something like "1 coin", or "612 coins."
                    sb.AppendFormat("{0} {1}", this.Value, this.Value > 1 ? SEconomyPlugin.Configuration.MoneyConfiguration.MoneyNamePlural : SEconomyPlugin.Configuration.MoneyConfiguration.MoneyName);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns a long representation of this Money object.
        /// </summary>
        public string ToLongString(bool ShowNegativeSign = false) {
            lock (__staticLock) {
                StringBuilder sb = new StringBuilder();
                long moneyCopy = 0L;

                //atomic copy
                Interlocked.Exchange(ref moneyCopy, this);

                //Negative balances still need to display like they are positives
                if (moneyCopy < 0) {
                    if (ShowNegativeSign) {
                        sb.Append("-");
                    }

                    Interlocked.Exchange(ref moneyCopy, moneyCopy * (-1));
                }
                
                if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {
                    if (((Money)moneyCopy).Platinum > 0) {
                        sb.AppendFormat("{0} {1}", ((Money)moneyCopy).Platinum, SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant4FullName);
                    }
                    if (((Money)moneyCopy).Gold > 0) {
                        sb.AppendFormat("{1}{0} {2}", ((Money)moneyCopy).Gold, sb.Length > 0 ? " " : "", SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant3FullName);
                    }
                    if (((Money)moneyCopy).Silver > 0) {
                        sb.AppendFormat("{1}{0} {2}", ((Money)moneyCopy).Silver, sb.Length > 0 ? " " : "", SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant2FullName);
                    }

                    if (((Money)moneyCopy).Copper > 0 || ((Money)moneyCopy)._moneyValue == 0) {
                        sb.AppendFormat("{1}{0} {2}", ((Money)moneyCopy).Copper, sb.Length > 0 ? " " : "", SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant1FullName);
                    }
                } else {
                    //Used in singular format: produces something like "1 coin", or "612 coins."
                    sb.AppendFormat("{0} {1}", this.Value, this.Value > 1 ? SEconomyPlugin.Configuration.MoneyConfiguration.MoneyNamePlural : SEconomyPlugin.Configuration.MoneyConfiguration.MoneyName);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Tries to parse Money out of a money representation.
        /// </summary>
        /// <param name="MoneyRepresentation">The money representation string, eg "1p1g", or "30s20c"</param>
        /// <param name="money">Reference to the money variable to parse to.</param>
        /// <returns>true if the parsing succeeded.</returns>
        public static bool TryParse(string MoneyRepresentation, out Money Money) {
            bool succeeded = false;

            try {
                Money = Parse(MoneyRepresentation);

                succeeded = true;
            } catch {
                //any exception marks a failed conversion, the reference must be set to 0
                succeeded = false;

                Money = 0;
            } 
            
            return succeeded;
        }

        /// <summary>
        /// Parses a money representation into a Money object.  Will throw exception if parsing fails.
        /// </summary>
        /// <param name="MoneyRepresentation">The money representation string, eg "1p1g", or "30s20c"</param>
        /// <returns>The money object parsed.  If not it'll return a big fat exception back in your face. :)</returns>
        public static Money Parse(string MoneyRepresentation) {
            lock (__staticLock) {
                long totalMoney = 0;

                if (SEconomyPlugin.Configuration.MoneyConfiguration.UseQuadrantNotation) {

                    if (!string.IsNullOrWhiteSpace(MoneyRepresentation) && new Regex(string.Format(@"{0}|{1}|{2}|{3}", SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant4Abbreviation,
                            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant3Abbreviation,
                            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant2Abbreviation,
                            SEconomyPlugin.Configuration.MoneyConfiguration.Quadrant1Abbreviation)).IsMatch(MoneyRepresentation)) {
                        Match moneyMatch = moneyRegex.Match(MoneyRepresentation);
                        long plat = 0, gold = 0, silver = 0, copper = 0;
                        string signedness = "";

                        if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[1].Value))
                            signedness = moneyMatch.Groups[1].Value;

                        if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[2].Value))
                            plat = long.Parse(moneyMatch.Groups[3].Value);

                        if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[4].Value))
                            gold = long.Parse(moneyMatch.Groups[5].Value);

                        if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[6].Value))
                            silver = long.Parse(moneyMatch.Groups[7].Value);

                        if (!string.IsNullOrWhiteSpace(moneyMatch.Groups[8].Value))
                            copper = long.Parse(moneyMatch.Groups[9].Value);

                        totalMoney += plat * ONE_PLATINUM;
                        totalMoney += gold * ONE_GOLD;
                        totalMoney += silver * ONE_SILVER;
                        totalMoney += copper;

                        //you can specify a minus at the start to indicate a negative amount.
                        if (!string.IsNullOrWhiteSpace(signedness)) {
                            totalMoney = -totalMoney;
                        }
                    } else {
                        //Attempt a plain conversion from a whole integer
                        long.TryParse(MoneyRepresentation, out totalMoney);
                    }
                } else {
                    if (numberRegex.IsMatch(MoneyRepresentation)) {
                        Match numberMatch = numberRegex.Match(MoneyRepresentation);
                        if (numberMatch.Groups.Count > 1) {
                            //Attempt a plain conversion from a whole integer
                            long.TryParse(numberMatch.Groups[1].Value, out totalMoney);
                        }
                    }
                }

                return totalMoney;
            }
        }
    }
}
