using System;
using System.Threading;
using ror.schema.upload;
using UnityEngine;

namespace Grapeshot {
  public class WifiOnlyRate : ApplicationRate {
    private readonly TimeSpan _updateTime;

    public WifiOnlyRate(TimeSpan updateTime) {
      _updateTime = updateTime;
    }
     
    public override void applicationClaimTicket(UploadChunkRequest chunkRequest, RateTicketReturn rateTicketReturn) {
      // Wait indefinetly for internet.
      while (!areFreeTickets()) {
        // Check every 15 seconds.
        Thread.Sleep(_updateTime);
      }

      applicationTryClaimTicket(chunkRequest, rateTicketReturn);
    }

    public override void applicationTryClaimTicket(UploadChunkRequest chunkRequest,
                                                   RateTicketReturn rateTicketReturn) {
      if (areFreeTickets()) {
        // Tickets must be allocated and held from the C# side until the C++ side has a safe reference to it.
        // When the C# side is done we must call dispose! Otherwise the ticket will only be freed when GC is
        // called.
        using (var rateTicket = new RateTicket()) { rateTicketReturn.ret(rateTicket); }
      }
    }

    public override bool areFreeTickets() {
      return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
    }
  }
}