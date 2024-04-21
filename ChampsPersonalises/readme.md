**Cache et champs personnalisés**

J'ai essayé de simplifier au maximum le code the CustomFieldRequest. 

Grace au `MemoryCache` et `GetOrCreate`, il est possible de simplement fournir une methode creation de l'entree du cache
qui sera appelée lors d'un *cache miss*.

Pour la configuration du `MemoryCache` on limite la taille du cache pour eviter tout probleme de MemoryLeak ainsi que 
une sliding expiration.

Le Sliding Expiration est interessant dans le sens ou le cache sera invalidé manuellement donc on assume que a partir
du moment ou la valeur est en cache, elle est correct tout en fournissant a MemoryCache un moyen efficate de liberer
de la place dans le Cache en cas de besoin.