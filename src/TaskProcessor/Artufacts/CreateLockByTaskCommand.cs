namespace Matrix.Application.Features.Locks;

public sealed record CreateLockByTaskCommand(
    int TaskKey, 
    long QueryId, 
    short M,
    string ProfileAddr,
    string ProfileOwnerAddr,
    string SourceAddr,
    string ParentAddr, 
    short Pos): ICommand;

internal sealed class CreateLockByTaskCommandHandler : ICommandHandler<CreateLockByTaskCommand>
{
    public Task<Result> Handle(CreateLockByTaskCommand request, CancellationToken cancellationToken)
    {
        
        // const lockPosPayload = taskVal.payload as MultiTaskLockPosPayload;
        //   const placeAddr = this.toFriendly(lockPosPayload.pos.parent);
        //   const lockedPos = lockPosPayload.pos.pos;
        //
        //   // validation
        //   const profileData = await fetchProfileData(taskVal.profile);
        //   if (!profileData || !profileData.owner)
        //   {
        //     await this.logLockErr("failed to load profile data", taskKey, taskVal);
        //     await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //     return false;
        //   }
        //
        //   if (this.toFriendly(lockPosPayload.source) != this.toFriendly(profileData.owner))
        //   {
        //     await this.logLockErr("unauthorized sender", taskKey, taskVal);
        //     await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //     return false;
        //   }
        //
        //   const rootPlace = await this.findRootPlace(taskVal.m, taskVal.profile);
        //   if (!rootPlace)
        //   {
        //       await this.logLockErr("failed to fetch root place", taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return false;
        //   }
        //
        //   const placeRow = await placesRepository.getPlaceByAddress(placeAddr);
        //   if (!placeRow)
        //   {
        //       await this.logLockErr("failed to get place", taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return false;
        //   }
        //
        //   if (placeRow.m != taskVal.m)
        //   {
        //       await this.logLockErr(`place is in the diff matrix`, taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return true;
        //   }
        //
        //   if (!placeRow.mp.startsWith(rootPlace.mp))
        //   {
        //       await this.logLockErr("place is in the diff subtree", taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return false;
        //   }
        //
        //   if (placeRow.filling == 0)
        //   {
        //       await this.logLockErr("empty place", taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return false;
        //   }
        //
        //   const profileAddr = this.toFriendly(taskVal.profile);
        //
        //   const existingLock = await locksRepository.getLockByPlaceAddrAndLockedPos(placeAddr, lockedPos, profileAddr);
        //   if (existingLock)
        //   {
        //       await this.logLockErr(`duplicate lock`, taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       await logger.info(`[TaskProcessor] last task key=${taskKey} successfully processed`);
        //       await logger.info('----------------------------------------------------------------------');
        //       return true;
        //   }
        //
        //   const otherPosLock = await locksRepository.getLockByPlaceAddrAndLockedPos(placeAddr, lockedPos == 0 ? 1 : 0, profileAddr);
        //   if (otherPosLock)
        //   {
        //       await this.logLockErr(`sibling pos is already locked`, taskKey, taskVal);
        //       await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //       return false;
        //   }
        //
        //
        //   // processing
        //   const createResult = await this.createLockFromTask(taskKey, taskVal, placeRow, lockedPos);
        //
        //   await this.cancelTask(rawMultiAddress, taskKey, taskVal);
        //
        //   await locksRepository.updateLockConfirm(createResult.id);
        //   await logger.info(`[TaskProcessor] updated lock #${createResult.id} with confirmed`);
        
        
        throw new NotImplementedException();
    }
}