d <- read.table("/Users/janusdybulla/Downloads/20180530/veg_knp_withfire_RCP85_now.dat")

# sort data file in respect to x and y coordinate
xx <- sort(unique(d[,1]))
yy <- rev(sort(unique(d[,2])))

N <- 4 # devide each cell by N
cellSize <- 0.5 / N

for ( tt in c(1989:2003) )
{
  # initialize result matrices with NA
  lgb <- matrix( NA, length(yy)*N, length(xx)*N )  # live grass biomass
  # iterate by x,y coords and fill result matrices
  for ( x in 1:length(xx) )
  {
    for ( y in 1:length(yy) )
    {
      # select relevant rows from data file (x,y and year)
      b <- subset( d, d[,1]==xx[x] & d[,2]==yy[y] & d[,3]==tt )
      # select column to show (10=bio)
      #h <- mean(b[,4], na.rm=T)
      h <- b[,4]
      for ( ncol in 1:N )
      {
        for ( nrow in 1:N )
        {
          lgb[nrow+y*N-N,ncol+x*N-N] <- h * 250000 / (N*N) # km2 -> ha
        }
      }
    }
  }
  
  print(lgb)
  # create .asc file

  filename <- paste("/Users/janusdybulla/Downloads/20180530/veg_knp_withfire_RCP85_now-gen/",
                    tt,".asc", sep ="")
  sink(filename)
  cat(sprintf("ncols %i",length(xx)*N),sep="\n")
  cat(sprintf("nrows %i",length(yy)*N),sep="\n")
  cat("xllcorner 30.75",sep="\n")
  cat("yllcorner -25.25",sep="\n")
  cat(sprintf("cellsize %f",cellSize),sep="\n")
  cat("nodata_value -9999",sep="\n")
  for ( y in 1:(length(yy)*N))
  {
    cat(lgb[y,])
    cat("\n")
  }
  sink()
}