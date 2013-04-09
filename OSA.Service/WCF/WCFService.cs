﻿namespace WCF
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.ServiceModel;
    using MySql.Data.MySqlClient;
    using OSAE;

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFService" in both code and config file together.
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class WCFService : IWCFService
    {
        private const string sourceName = "WCF Service";

        /// <summary>
        /// Provides access to logging
        /// </summary>
        Logging logging = Logging.GetLogger();
      
        public event EventHandler<CustomEventArgs> MessageReceived;

        #region iWCFService Members

        private static readonly List<IMessageCallback> subscribers = new List<IMessageCallback>();

        public void SendMessageToClients(OSAEWCFMessage message)
        {
            try
            {
                subscribers.ForEach(delegate(IMessageCallback callback)
                {
                    if (((ICommunicationObject)callback).State == CommunicationState.Opened)
                    {
                        try
                        {
                            callback.OnMessageReceived(message);
                        }
                        catch (TimeoutException ex)
                        {
                            logging.AddToLog("Timeout error when sending message to client: " + ex.Message, true);
                            subscribers.Remove(callback);
                        }
                        catch (Exception ex)
                        {
                            logging.AddToLog("Error when sending message to client: " + ex.Message, true);
                        }
                    }
                    else
                    {
                        subscribers.Remove(callback);
                    }
                });
            }
            catch (TimeoutException ex)
            {
                logging.AddToLog("Timeout Exception Error in SendMessageToClients: " + ex.Message, true);
            }
            catch (Exception ex)
            {
                logging.AddToLog("Error in SendMessageToClients: " + ex.Message, true);
            }
        }

        public bool Subscribe()
        {
            logging.AddToLog("Attempting to add a subscriber", true);
            try
            {
                IMessageCallback callback = OperationContext.Current.GetCallbackChannel<IMessageCallback>();
                if (!subscribers.Contains(callback))
                {
                    subscribers.Add(callback);
                    logging.AddToLog("New subscriber: " + callback.ToString(), true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Unsubscribe()
        {
            try
            {
                IMessageCallback callback = OperationContext.Current.GetCallbackChannel<IMessageCallback>();
                if (!subscribers.Contains(callback))
                    subscribers.Remove(callback);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void messageHost(OSAEWCFMessageType msgType, string message, string from)
        {
            try
            {
                if (MessageReceived != null)
                {
                    OSAEWCFMessage msg = new OSAEWCFMessage();
                    msg.Type = msgType;
                    msg.Message = message;
                    msg.From = OSAE.Common.ComputerName;
                    msg.TimeSent = DateTime.Now;
                    MessageReceived(null, new CustomEventArgs(msg));
                }
            }
            catch
            {

            }
        }

        public OSAEObjectCollection GetAllObjects()
        {
            MySqlCommand command = new MySqlCommand("SELECT object_name, object_description, object_type_description, container_name, state_label, last_updated, address, enabled, time_in_state, base_type FROM osae_v_object");
            DataSet ds = OSAESql.RunQuery(command);

            OSAEObjectCollection objs = new OSAEObjectCollection();
            OSAEObject obj;

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                obj = new OSAEObject(dr["object_name"].ToString(), dr["object_description"].ToString(), dr["object_type_description"].ToString(), dr["address"].ToString(), dr["container_name"].ToString(), Int32.Parse(dr["enabled"].ToString()));
                obj.LastUpd = dr["last_updated"].ToString();
                obj.State.Value = dr["state_label"].ToString();
                obj.State.TimeInState = long.Parse(dr["time_in_state"].ToString());
                obj.BaseType = dr["base_type"].ToString();
                objs.Add(obj);
            }
            return objs;
        }

        public OSAEObject GetObject(string name)
        {
            // lookup object 
            return OSAEObjectManager.GetObjectByName(name);
        }

        public OSAEObject GetObjectByAddress(string address)
        {
            // lookup object 
            return OSAEObjectManager.GetObjectByAddress(address);
        }

        public OSAEObjectCollection GetObjectsByType(string type)
        {
            return OSAEObjectManager.GetObjectsByType(type);
        }

        public OSAEObjectCollection GetObjectsByBaseType(string type)
        {
            // lookup objects of the requested type             
            return OSAEObjectManager.GetObjectsByBaseType(type);
        }

        public OSAEObjectCollection GetObjectsByContainer(string container)
        {
            return OSAEObjectManager.GetObjectsByContainer(container);
        }

        public Boolean ExecuteMethod(string name, string method, string param1, string param2)
        {
            // execute a method on an object 
            OSAEMethodManager.MethodQueueAdd(name, method, param1, param2, sourceName);
            return true;
        }

        public Boolean SendPattern(string pattern)
        {
            string patternName = Common.MatchPattern(pattern);
            if (!string.IsNullOrEmpty(patternName))
            {
                OSAEScriptManager.RunPatternScript(patternName, "", sourceName);
            }
            return true;
        }

        public Boolean AddObject(string name, string description, string type, string address, string container, string enabled)
        {
            OSAEObjectManager.ObjectAdd(name, description, type, address, container, Convert.ToBoolean(enabled));

            return true;
        }

        public Boolean UpdateObject(string oldName, string newName, string description, string type, string address, string container, int enabled)
        {
            OSAEObjectManager.ObjectUpdate(oldName, newName, description, type, address, container, enabled);

            return true;
        }

        public Boolean DeleteObject(string name)
        {
            OSAEObjectManager.ObjectDelete(name);

            return true;
        }

        //public Boolean AddScript(string objName, string objEvent, string script)
        //{
        //    OSAEScriptManager.ObjectEventScriptAdd(objName, objEvent, script);
        //    return true;
        //}

        //public Boolean UpdateScript(string objName, string objEvent, string script)
        //{
        //    OSAEScriptManager.ObjectEventScriptUpdate(objName, objEvent, script);
        //    return true;
        //}

        public OSAEObjectCollection GetPlugins()
        {
            OSAEObjectManager objectManager = new OSAEObjectManager();

            // lookup objects of the requested type 
            OSAEObjectCollection objects = OSAEObjectManager.GetObjectsByBaseType("plugin");
            return objects;
        }

        public DataSet ExecuteSQL(string sql)
        {
            MySqlCommand command = new MySqlCommand(sql);
            return OSAESql.RunQuery(command);
        }
        #endregion

        public Boolean SetProperty(string objName, string propName, string propValue)
        {
            OSAEObjectPropertyManager.ObjectPropertySet(objName, propName, propValue, sourceName);

            return true;
        }

        public Boolean SetState(string objName, string state)
        {
            OSAEObjectStateManager.ObjectStateSet(objName, state, sourceName);

            return true;
        }

    }

    public class CustomEventArgs : EventArgs
    {
        public OSAEWCFMessage Message;

        public CustomEventArgs(OSAEWCFMessage message)
        {
            Message = message;
        }

    }

    public class OSAEWCFMessage
    {
        public OSAEWCFMessageType Type { get; set; }
        public string From { get; set; }
        public string Message { get; set; }
        public DateTime TimeSent { get; set; }
    }

    public enum OSAEWCFMessageType
    {
        PLUGIN = 1,
        LOG = 2,
        CONNECT = 3,
        CMDLINE = 4,
        METHOD = 5,
        UPDATESCREEN = 6,
        SERVICE = 7


    }
}
