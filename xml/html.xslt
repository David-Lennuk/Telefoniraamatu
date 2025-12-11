<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:param name="name"/>
  <xsl:template match="/">
    <html>
      <head>
        <meta charset="UTF-8"/>
        <title>Kontaktid</title>
      </head>
      <body>
        <table border="1" cellspacing="0" cellpadding="4">
          <tr>
            <th>ID</th>
            <th>Nimi</th>
            <th>Perekonnanimi</th>
            <th>Telefon</th>
            <th>Märkmed</th>
          </tr>
          <xsl:for-each select="contacts/contact[ (string-length($name)=0 or contains(translate(nimi, 'ABCDEFGHIJKLMNOPQRSTUVWXYZАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ', 'abcdefghijklmnopqrstuvwxyzабвгдеёжзийклмнопрстуфхцчшщъыьэюя'), translate($name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ', 'abcdefghijklmnopqrstuvwxyzабвгдеёжзийклмнопрстуфхцчшщъыьэюя')) ) ]">
            <tr>
              <td><xsl:value-of select="@id"/></td>
              <td><xsl:value-of select="nimi"/></td>
              <td><xsl:value-of select="perekonnanimi"/></td>
              <td><xsl:value-of select="telefon"/></td>
              <td><xsl:value-of select="markmed"/></td>
            </tr>
          </xsl:for-each>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
