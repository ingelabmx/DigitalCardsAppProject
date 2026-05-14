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
    public class AdminConnector
    {
        static string connStr = ConfigurationManager.ConnectionStrings["DCConnectionString"].ConnectionString;    
        public static DataTable GetBusinessData()
        {           
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand("spGetBusinessData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

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

        public static void InsertBusinessData(AdminDetails ADetails)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spInsertBusinessData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_Name", ADetails.BusName);
                        cmd.Parameters.AddWithValue("Business_Password", ADetails.BusPassword);
                        cmd.Parameters.AddWithValue("Business_Email", ADetails.BusEmail);
                        cmd.Parameters.AddWithValue("Business_Logo", ADetails.BusLogo);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting business data: {ex.Message}", ex);
            }
        }

        public static DataTable GetBusinessDataDetails(string BusNo)
        {
            DataTable dt = new DataTable();

            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spGetBusinessDetails", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_ID", BusNo);

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
        
        public static void ModifyBusinessData(string busNo, AdminDetails ADetails)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spModifyBusinessData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_ID", busNo);
                        cmd.Parameters.AddWithValue("Business_Name", ADetails.BusName);
                        cmd.Parameters.AddWithValue("Business_Password", ADetails.BusPassword);
                        cmd.Parameters.AddWithValue("Business_Email", ADetails.BusEmail);
                        cmd.Parameters.AddWithValue("Business_Logo", ADetails.BusLogo);

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
        public static void DeleteBusinessData(string busNo, AdminDetails ADetails)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    using (MySqlCommand cmd = new MySqlCommand("spDeleteBusinessData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("Business_ID", busNo);
                        cmd.Parameters.AddWithValue("@Business_Name", ADetails.BusName);
                        cmd.Parameters.AddWithValue("@Business_Email", ADetails.BusEmail);

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
        public static string HashPassword(string BusPassword)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(BusPassword));
                return Convert.ToBase64String(bytes);
            }
        }
        public static bool ValidateLogin(string BusEmail, string BusPassword)
        {
            try
            {
                // Crear la conexión a la base de datos
                using (MySqlConnection con = new MySqlConnection(connStr))
                {
                    // Abrir la conexión
                    con.Open();

                    // Consulta SQL para verificar si el usuario existe y obtener la contraseña almacenada
                    string query = "SELECT BusPassword FROM Bussiness WHERE BusEmail = @BusEmail";

                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BusEmail", BusEmail);

                        // Obtener el hash de la contraseña almacenado en la base de datos
                        string storedPasswordHash = cmd.ExecuteScalar() as string;

                        if (storedPasswordHash != null)
                        {
                            // Comparar la contraseña proporcionada con el hash almacenado
                            string hashedPassword = HashPassword(BusPassword);

                            return hashedPassword == storedPasswordHash;
                        }
                        else
                        {
                            // Si el usuario no existe
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}