namespace EndlessGame.Controllers
{
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.AspNetCore.SignalR;

  using EndlessGame.Entities;
  using EndlessGame.HubConfig;
  using System.Numerics;
  using Microsoft.EntityFrameworkCore.Metadata.Internal;

  [Route("api/[controller]")]
  [ApiController]
  public class ChatController : ControllerBase
  {
    private readonly IHubContext<ChatHub> _hub;
    private readonly EndlessGameDBContext context;

    public ChatController(IHubContext<ChatHub> hub, EndlessGameDBContext _context)
    {
      _hub = hub;
      context = _context;
    }

    [HttpGet]
    public IActionResult Get()
    { 
      var maxScore = context.Users.Select(s => BigInteger.Parse(s.Score.ToString())).ToList().Max();
      var champ = context.Users.FirstOrDefault(s => BigInteger.Parse(s.Score.ToString()) == maxScore);

      _hub.Clients.All.SendAsync("score", champ);
      return Ok(new { Message = "Request Completed" });
    }
     
    [HttpPost("user/")]
    public IActionResult User([FromBody] UserBindingModel dat)
    { 
      var longScore = BigInteger.Parse(dat.Score.ToString());
      
      var users = context.Users.AsQueryable(); 
      var maxScore2 = users.Select(s => BigInteger.Parse(s.Score.ToString())).ToList().Max();
      
      var champion = context.Users.FirstOrDefault(s => s.Score == maxScore2.ToString());

      var user2 = users.FirstOrDefault(u => u.Username == dat.Username);
      if (user2 == null)
      {
        context.Add(new User { Score = dat.Score, Username = dat.Username, History = "" });
        var saved = context.SaveChanges();
        return Ok();
      }

      var p = BigInteger.Parse(user2.Score.ToString());
      var p2 = BigInteger.Parse(champion.Score.ToString());

      var champion2 = new UserViewModel() { Username = champion.Username, History = champion.History, Score = p2.ToString() };
      var user = new User() { Username = user2.Username, History = user2.History, Score = p.ToString() };

      var history = dat.History != "|" ? dat.History : "";
      if (longScore > maxScore2)
      {
        var champ = new UserViewModel { Username = dat.Username, Score = longScore.ToString(), History = history };
        _hub.Clients.All.SendAsync("score", champ);
      }

      else
      {
        _hub.Clients.All.SendAsync("score", champion);
      }

      if (user != null && BigInteger.Parse(user2.Score.ToString()) >= longScore)
      {
        var currentUser = new UserViewModel { Username = user.Username, Score = user.Score, History = user.History };
        return Ok(currentUser);
      }

      if (user != null && BigInteger.Parse(user2.Score.ToString()) < longScore)
      {
        user2.Score = longScore.ToString();
        user2.History = user.History + history;
        context.SaveChanges();
        return Ok(dat.Score);
      }

      else
      {
        context.Add(new User { Score = longScore.ToString(), Username = dat.Username, History = history });
         context.SaveChanges();
        return Ok(dat.Score);
      }
    }

  }
}
