using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TestMVC.Models
{ 
    public class UserHubModels
    {
        public string UserName { get; set; }
        public HashSet<string> ConnectionIds { get; set; }
    }
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserHubModels> Users =
       new ConcurrentDictionary<string, UserHubModels>(StringComparer.InvariantCultureIgnoreCase);

        static int Temperature; 

      
        public void SendMessage(string SentTo, string message)
        {
           // Temperature = temperature;

            //Clients is ConnectionContext, it holds the information about all the connection.
            //Others in 'Clients.Others' is holding the list of all connected user except the caller user (the user which has 
            //called this method) 
            //NotifyUser is a function on the clientside, you will understand it later.
          //  Clients.Others.NotifyUser(temperature);

            //Send To  
            UserHubModels receiver; 
            if (Users.TryGetValue(SentTo, out receiver))
            {
              //  var cid = receiver.ConnectionIds.FirstOrDefault();
                var context = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
                foreach (var cid in receiver.ConnectionIds) {
                    context.Clients.Client(cid).NotifyUser(message);
                }
                
            }

        }
         
        //public void GetWeather()
        //{
        //    Clients.Caller.NotifyUser(Temperature);
        //}

        public override Task OnConnected()
        {
            string userName = Context.QueryString["username"];
            string connectionId = Context.ConnectionId;

            var user = Users.GetOrAdd(userName, _ => new UserHubModels
            {
                UserName = userName,
                ConnectionIds = new HashSet<string>()
            });

            lock (user.ConnectionIds)
            {
                user.ConnectionIds.Add(connectionId);
                if (user.ConnectionIds.Count == 1)
                {
                    Clients.Others.userConnected(userName);
                }
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
       {
            string userName = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;

            UserHubModels user;
            Users.TryGetValue(userName, out user);

            if (user != null)
            {
                lock (user.ConnectionIds)
                {
                    user.ConnectionIds.RemoveWhere(cid => cid.Equals(connectionId));
                    if (!user.ConnectionIds.Any())
                    {
                        UserHubModels removedUser;
                        Users.TryRemove(userName, out removedUser);
                        Clients.Others.userDisconnected(userName);
                    }
                }
            }

            return base.OnDisconnected(stopCalled);
        }

    }
}