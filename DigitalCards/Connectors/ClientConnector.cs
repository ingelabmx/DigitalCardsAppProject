using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Configuration;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cmp;
using DigitalCardsApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace DigitalCardsApp.Connectors
{
    public class ClientConnector
    {
        static string connStr = ConfigurationManager.ConnectionStrings["DCConnectionString"].ConnectionString;

        public static void InsertClientData(ClientDetails CDetails)
        {
            using (var connection = new MySqlConnection(connStr))
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(connStr))
                    {
                        using (MySqlCommand cmd = new MySqlCommand("spInsertUserClientData", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("User_Name", CDetails.UsName);
                            cmd.Parameters.AddWithValue("User_Password", CDetails.UsPassword);
                            cmd.Parameters.AddWithValue("First_Name", CDetails.UsFirstName);
                            cmd.Parameters.AddWithValue("Last_Name", CDetails.UsLastName);
                            cmd.Parameters.AddWithValue("User_Email", CDetails.UsEmail);

                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                
                }                
                catch (Exception ex)
                {
                    throw new ArgumentException("User_Name, User_Password, First_Name, LastName and User_Email cannot be null or empty.");
                }
            }
        }

        public static ClientDetails ClientLogin(ClientDetails CDetails)
        {
            using (var connection = new MySqlConnection(connStr))
            {
                try
                {
                    using (var cmd = new MySqlCommand("spSelectUserClientData", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("User_Identifier", CDetails.UsName); // Username or Email
                        cmd.Parameters.AddWithValue("User_Password", CDetails.UsPassword);

                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ClientDetails
                                {
                                    UsID = reader.GetInt32("UserID"),
                                    UsName = reader.GetString("UserName"),
                                    UsFirstName = reader.GetString("FirstName"),
                                    UsLastName = reader.GetString("LastName"),
                                    UsEmail = reader.GetString("UserEmail"),
                                    UsRole = reader.GetInt32("RoleID")
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al iniciar sesión.", ex);
                }
            }

            return null;
        }

        public static bool UpdateClientPassword(string email, string hashedPassword)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spUpdateClientPassword", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure; // Specify the command type
                        cmd.Parameters.AddWithValue("@User_Email", email);
                        cmd.Parameters.AddWithValue("@User_Password", hashedPassword);

                        con.Open();
                        return cmd.ExecuteNonQuery() > 0; // Returns true if rows are affected
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception for debugging (optional)
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool DoesEmailExist(string UserEmail)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spCheckIfEmailExist", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@User_Email", UserEmail); // Ensure parameter name matches stored procedure
                        con.Open();

                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public static void DeletePasswordResetToken(string email)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spDeletePasswordResetToken", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@User_Email", email);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void StorePasswordResetToken(string email, string token, DateTime expiration)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spStorePasswordResetToken", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Adding parameters for the stored procedure
                        cmd.Parameters.AddWithValue("User_Email", email);
                        cmd.Parameters.AddWithValue("Token_Value", token);
                        cmd.Parameters.AddWithValue("Expiration_Time", expiration);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error storing password reset token: {ex.Message}");
                throw new Exception("An error occurred while storing the password reset token.", ex);
            }
        }

        public static bool IsResetTokenValid(string token)
        {
            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                using (MySqlCommand cmd = new MySqlCommand("spValidateResetToken", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("Token_Value", token);

                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0; // Returns true if token is valid
                }
            }
        }

        public static string GetEmailByToken(string token)
        {
            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                using (MySqlCommand cmd = new MySqlCommand("spGetEmailByToken", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("Token_Value", token);

                    con.Open();
                    return cmd.ExecuteScalar()?.ToString();
                }
            }
        }
        public static string GenerateCheckFigures(int checkQty, int totalChecks = 10)
        {
            StringBuilder sb = new StringBuilder();

            // Añadir los círculos verdes por cada "checada" completa
            for (int i = 0; i < checkQty; i++)
            {
                sb.Append("<span style='color:green; font-size:20px;'>●</span> "); // Círculo verde
            }

            // Añadir los círculos grises por cada "checada" incompleta
            for (int i = checkQty; i < totalChecks; i++)
            {
                sb.Append("<span style='color:gray; font-size:20px;'>●</span> "); // Círculo gris
            }

            return sb.ToString();
        }

        public static List<Card> GetClientCards(int userId)
        {
            List<Card> cards = new List<Card>();

            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                using (MySqlCommand cmd = new MySqlCommand("spGetClientCards", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    con.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Obtener el número de "checadas" (por ejemplo, CheckQTY) y formatear la visualización
                            int checkQty = Convert.ToInt32(reader["CheckQTY"]);
                            string checkDisplay = GenerateCheckFigures(checkQty);

                            cards.Add(new Card
                            {
                                Title = reader["BusinessName"].ToString(),
                                Description = $"Creado el {Convert.ToDateTime(reader["CreationDate"]).ToShortDateString()}<br /> Visitas: {checkDisplay}"
                            });
                        }
                    }
                }
            }

            return cards;
        }
        public class Card
    {
            public string Title { get; set; }
            public string Description { get; set; }
    }

    }   
    
}