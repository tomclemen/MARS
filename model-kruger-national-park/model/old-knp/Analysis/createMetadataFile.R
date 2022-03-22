# number of days per month
mlen <- c( 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 )

filename <- "~/Downloads/prec/metadata.csv"
sink(filename)

cat("Date;File",sep="\n") # header of metadata.csv

yr_start <- 2031
yr_end <- 2031

for ( year in yr_start:yr_end )
{
  for ( month in 1:12 )
  {
    for ( day in 1:mlen[month] )
    {
      tstamp <- paste(year, "-", month, "-", day, "T00:00:00;", sep="")
      prec_file <- paste(year, "_", month, "_", day, "_pre.asc", sep="")
      file_row <- paste(tstamp, prec_file, sep="")
      cat(file_row,sep="\n")
    }
  }
}

sink()

filename <- "~/Downloads/temp/metadata.csv"
sink(filename)

cat("Date;File",sep="\n") # header of metadata.csv

for ( year in yr_start:yr_end )
{
  for ( month in 1:12 )
  {
    for ( day in 1:mlen[month] )
    {
      tstamp <- paste(year, "-", month, "-", day, "T00:00:00;", sep="")
      tmp_file <- paste(year, "_", month, "_", day, "_temp.asc", sep="")
      file_row <- paste(tstamp, tmp_file, sep="")
      cat(file_row,sep="\n")
    }
  }
}

sink()

