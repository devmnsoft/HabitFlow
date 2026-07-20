export function toggleHabit(id){return fetch(`/habits/${id}/toggle`,{method:'POST'});}
