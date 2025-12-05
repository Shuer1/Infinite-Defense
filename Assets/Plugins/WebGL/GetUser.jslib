mergeInto(LibraryManager.library, {
  GetWallUser: function(){
    var u = window.WALL_USER || {userId:'',token:'',taskId:''};
    return allocateUTF8(JSON.stringify(u));
  }
});