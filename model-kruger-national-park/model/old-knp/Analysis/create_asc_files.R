# please make sure prec/temp folder exists under Downloads


xllcorner    <- 30.5
yllcorner    <- -26
ncols        <- 4
nrows        <- 8
cellsize     <- 0.5
nodata_value <- -9999

alldat <- NULL

xseq <- seq( xllcorner+0.25, length=ncols, by=cellsize )
yseq <- seq( yllcorner+0.25, length=nrows, by=cellsize )

mlen <- c( 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 )

for ( x in 1:length(xseq) )
{
  for ( y in 1:length(yseq) ) 
  {
    fname <- paste( "~/ownCloud/MARS_Modelle/MARS_KNP/RCP Daten/climate_RCP45/sa_MPI_RCP45_b_", xseq[x], "_", yseq[y], ".dat", sep="" )
    print(fname)
    d <- read.table(fname)
    g <- cbind( rep( xseq[x], dim(d)[1]), rep( yseq[y], dim(d)[1]), d )
    alldat <- rbind( alldat, g )
  }
}

for ( year in 2031:2031 )
{
  for ( month in 1:12 )
  {
    for ( day in 1:mlen[month] )
    {
      print( c( year, month, day ) )
      mpre <- matrix( -0, ncol=ncols, nrow=nrows )
      mtmp <- matrix( -0, ncol=ncols, nrow=nrows )
      
      for ( x in 1:length(xseq) )
      {
        for ( y in 1:length(yseq) ) 
        {
          vpre <- subset( alldat, alldat[,1]==xseq[x] & alldat[,2]==yseq[y] & alldat[,3]==year & alldat[,4]==month & alldat[,5]==day )[,7]
          vpre <- max( vpre, 0 )
          vtmi <- subset( alldat, alldat[,1]==xseq[x] & alldat[,2]==yseq[y] & alldat[,3]==year & alldat[,4]==month & alldat[,5]==day )[,9]
          vtma <- subset( alldat, alldat[,1]==xseq[x] & alldat[,2]==yseq[y] & alldat[,3]==year & alldat[,4]==month & alldat[,5]==day )[,10]
          vtmp <- vtmi + (vtma-vtmi)/2
          
          mpre[y,x] <- vpre
          mtmp[y,x] <- vtmp
        }
      }
      
      opre <- NULL
      opre <- rbind( opre, c( "ncols",        ncols,        rep("", ncols-2) ) )
      opre <- rbind( opre, c( "nrows",        nrows,        rep("", ncols-2) ) )
      opre <- rbind( opre, c( "xllcorner",    xllcorner,    rep("", ncols-2) ) )
      opre <- rbind( opre, c( "yllcorner",    yllcorner,    rep("", ncols-2) ) )
      opre <- rbind( opre, c( "cellsize",     cellsize,     rep("", ncols-2) ) )
      opre <- rbind( opre, c( "nodata_value", nodata_value, rep("", ncols-2) ) )
      opre <- rbind( opre, mpre )      

      otmp <- NULL
      otmp <- rbind( otmp, c( "ncols",        ncols,        rep("", ncols-2) ) )
      otmp <- rbind( otmp, c( "nrows",        nrows,        rep("", ncols-2) ) )
      otmp <- rbind( otmp, c( "xllcorner",    xllcorner,    rep("", ncols-2) ) )
      otmp <- rbind( otmp, c( "yllcorner",    yllcorner,    rep("", ncols-2) ) )
      otmp <- rbind( otmp, c( "cellsize",     cellsize,     rep("", ncols-2) ) )
      otmp <- rbind( otmp, c( "nodata_value", nodata_value, rep("", ncols-2) ) )
      otmp <- rbind( otmp, mtmp )      
      
      onamepre <- paste( "~/Downloads/prec/", year, "_", month, "_", day, "_pre.asc", sep="")
      onametmp <- paste( "~/Downloads/temp/", year, "_", month, "_", day, "_temp.asc", sep="")

      write.table( opre, onamepre, col.names=F, row.names=F, quote=F )
      write.table( otmp, onametmp, col.names=F, row.names=F, quote=F )

    }
  }
}





















