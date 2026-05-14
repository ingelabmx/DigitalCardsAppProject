using DigitalCardsApp.Models;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DigitalCardsApp.Connectors
{
    public class BusinessConnector
    {
        static string connStr = ConfigurationManager.ConnectionStrings["DCConnectionString"].ConnectionString;

        public static DataTable GetClientCardData(int BusinessID)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand("spGetCardDataBusiness", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return dt;
        }

        public static DataTable GetClientData()
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection con = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("spGetCardDataBusiness", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return dt;
        }

        public static BusinessDetails BusinessLogin(BusinessDetails BDetails)
        {
            using (var connection = new MySqlConnection(connStr))
            {
                try
                {
                    using (var cmd = new MySqlCommand("spSelectBusinessData", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_Email", BDetails.BusEmail);
                        cmd.Parameters.AddWithValue("Business_Password", BDetails.BusPassword);

                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BusinessDetails
                                {
                                    BusID = reader.GetInt32("BusinessID"),
                                    BusName = reader.GetString("BusinessName"),
                                    BusPassword = reader.GetString("BusinessPassword"),
                                    BusEmail = reader.GetString("BusinessEmail"),
                                    BusLogo = reader.GetString("BusinessLogo")
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while logging in.", ex);
                }
            }

            return null;
        }

        public static ClientDetails GetUserInfo(ClientDetails CDetails)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spGetUserClientInfo", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_Name", CDetails.UsName);

                        con.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ClientDetails
                                {
                                    UsID = reader[0] != DBNull.Value ? Convert.ToInt32(reader[0]) : 0,
                                    UsFirstName = reader[1]?.ToString(),
                                    UsLastName = reader[2]?.ToString(),
                                    UsEmail = reader[3]?.ToString(),
                                    UsName = CDetails.UsName
                                };
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static int GetUserID(ClientDetails CDetails)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spGetUserClientID", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_Name", CDetails.UsName);

                        con.Open();
                        var result = cmd.ExecuteScalar();
                        return (int)result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static DateTime GetCardCreatedTime(ClientDetails CDetails, int BusinessID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spGetCardCreatedTime", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_ID", CDetails.UsID);
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);

                        con.Open();
                        var result = cmd.ExecuteScalar();
                        return (DateTime)result;
                    }
                }
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        public static void InsertClientData(int UserID, int BusinessID, string IdBusinessCGoogle, out int rowsAffected)
        {
            rowsAffected = 0; // Default to 0 in case of exceptions
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spInsertCardData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("User_ID", UserID);
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);
                        cmd.Parameters.AddWithValue("Card_IDGoogle", IdBusinessCGoogle);

                        con.Open();
                        rowsAffected = cmd.ExecuteNonQuery(); // Capture the number of rows affected
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed
                throw;
            }
        }

        public static DataTable GetLast5Checks(int BusinessID)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand("spGetLast5Checks", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return dt;
        }

        public static void IncreaseCheckQTY(int UserID, int BusinessID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spIncreaseCheckQTY", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_ID", UserID);
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static CardsDetails GetCheckQTY(int UserID, int BusinessID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spGetCardDataChecks", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_ID", UserID);
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);

                        con.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new CardsDetails
                                {
                                    CheckQTY = reader[0] != DBNull.Value ? Convert.ToInt32(reader[0]) : 0,
                                    HistoricCheckQTY = reader[1] != DBNull.Value ? Convert.ToInt32(reader[0]) : 0,
                                    CardIDGoogle = reader[2]?.ToString(),
                                };
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DataTable GetYearData(int BusinessID)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand("spGetYearData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Pass parameters
                        cmd.Parameters.AddWithValue("Business_ID", BusinessID);
                        cmd.Parameters.AddWithValue("Current_Year", DateTime.Now.Year);

                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                throw new Exception("Error recuperando la información del año.", ex);
            }
            return dt;
        }
    }
}